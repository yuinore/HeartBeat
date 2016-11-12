using HatoBMSLib;
using HatoLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Simamu
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            // BMSStructWriterTestClass.Search(@"Some BMS Directory");

            // BMSStructWriterTestClass.TestOneFile(textBox_mergingBMS.Text, this);

            string bmsFilePath = textBox_mergingBMS.Text;
            string bmsDirectoryPath = Path.GetDirectoryName(bmsFilePath);

            string namingRule = textBox_namingRule.Text;

            if (!File.Exists(bmsFilePath))
            {
                MessageBox.Show(this, "BMSファイルが存在しません。設定を確認してください。", "Alert");
                return;
            }

            BMSStruct bms = new BMSStruct(bmsFilePath);

            //******** キー音を発音時刻順に並べ替えてグループ化する(GroupByを使え) ********
            Dictionary<Rational, HashSet<BMObject>> MeasureToObjectSet = new Dictionary<Rational, HashSet<BMObject>>();
            List<BMObject> NewSoundBMObjects = new List<BMObject>();

            foreach (var ev in bms.SoundBMObjects)
            {
                if (!bms.WavDefinitionList.ContainsKey(ev.Wavid) || bms.WavDefinitionList[ev.Wavid] == "")
                {
                    // オブジェはあるが、WAVが定義されていなかった（無音ノーツ等）
                    NewSoundBMObjects.Add(ev);  // 無音オブジェ
                    continue;
                }

                if (!MeasureToObjectSet.ContainsKey(ev.Measure))
                {
                    MeasureToObjectSet[ev.Measure] = new HashSet<BMObject>();
                }

                MeasureToObjectSet[ev.Measure].Add(ev);
            }

            //******** テーブルの並べ替えを行う ********
            //var KeySoundGroupListOrderByMeasure = MeasureToObjectSet.OrderBy(x => x.Key).Where(x => x.Value.Count >= 2).ToArray();
            var KeySoundGroupListOrderByMeasure = MeasureToObjectSet.OrderBy(x => x.Key).ToArray();

            //******** グループに対してUnique操作を行い、各セットに対して命名する。WAV定義リストを追加する。 ********
            HashSet<HashSet<string>> KeySoundHashSet = new HashSet<HashSet<string>>();
            List<HashSet<string>> KeySoundListOrderByMeasure = new List<HashSet<string>>();
            Dictionary<HashSet<string>, string> KeySoundGroupToWavFilename = new Dictionary<HashSet<string>, string>();
            Dictionary<HashSet<string>, int> KeySoundGroupToWavid = new Dictionary<HashSet<string>, int>();

            //int VacantWavid = 1 + bms.WavDefinitionList.Select(x => x.Key).Where(x => bms.LNObj == null || x != bms.LNObj).Max();  // 多分LNObjは含んでないと思うんですけど
            int VacantWavid = 1 + bms.WavDefinitionList.Select(x => x.Key).Max();

            int KeyGroupIndex = 0; // FIXME:

            foreach (var kvpair in KeySoundGroupListOrderByMeasure)
            {
                var MergedKeySoundNameList = kvpair.Value.Select(ev => bms.WavDefinitionList[ev.Wavid]).ToArray();

                if (MergedKeySoundNameList.Length <= 1)
                {
                    NewSoundBMObjects.Add(kvpair.Value.First());  // 同時発音数が1のキー音
                    continue;
                }

                var MergedKeySoundSet = new HashSet<string>(MergedKeySoundNameList);

                if (!KeySoundHashSet.Contains(MergedKeySoundSet))
                {
                    KeySoundHashSet.Add(MergedKeySoundSet);
                    KeySoundListOrderByMeasure.Add(MergedKeySoundSet);

                    // ここで命名を行う。
                    // todo: 既にファイルが存在するかどうかの確認
                    string newFilename = String.Format(namingRule, KeyGroupIndex++);

                    KeySoundGroupToWavFilename[MergedKeySoundSet] = newFilename;

                    bms.WavDefinitionList.Add(VacantWavid, newFilename);  // 定義リストの追加

                    KeySoundGroupToWavid[MergedKeySoundSet] = VacantWavid;
                    VacantWavid++;
                }

                string mergedSoundFilename = KeySoundGroupToWavFilename[MergedKeySoundSet];

                var ev01 = kvpair.Value.First();

                NewSoundBMObjects.Add(new BMObject(
                    ev01.BMSChannel,
                    ev01.BMSSubChannel,
                    KeySoundGroupToWavid[MergedKeySoundSet],
                    ev01.Measure
                    ));
            }

            //******** BMSファイルを修正し、書き出す。 ********
            NewSoundBMObjects.Sort();
            bms.SoundBMObjects = NewSoundBMObjects;

            bms.Export(bmsFilePath + "_______merged.bms");

            //******** 各グループのキー音を統合し、音声処理をする ********
            var batchText = "";
            var bmx2wavExecutablePath = textBox_bmx2wavPath.Text;

            foreach (var set in KeySoundListOrderByMeasure)
            {
                var targetWavFilename = KeySoundGroupToWavFilename[set];

                var bmsText1 = "*---- Autogenerated by Simamu ----\r\n#TITLE " + targetWavFilename + "\r\n\r\n";

                var bmsText2 = String.Join("\r\n", set.OrderBy(x => x).Select((x, index) =>
                {
                    try
                    {
                        return "#WAV" + BMConvert.ToBase36(index + 1) + " " + x;
                    }
                    catch
                    {
                        throw new Exception();
                    }
                })) + "\r\n\r\n";

                var bmsText3 = String.Join("\r\n", set.Select((x, index) => "#00001:" + BMConvert.ToBase36(index + 1))) + "\r\n";

                // 独自の音声処理アルゴリズムを用いるか BMX2WAV を用いるか微妙に悩む・・・
                // 独自のアルゴリズムを使ってもいいけどリサンプルの処理がめんどくさい（FIRじゃなくて双方向IIRでも良い説はある）

                // bmx2wav.ini を書き出す必要があるかもしれない

                // bmx2wavc.exe -c bmx2wav.ini input000.bms output000.wav

                // うーん、.batの仕様がわからない・・・あ、できた

                var tempBmsFilename = "#temp_bms_" + KeySoundGroupToWavFilename[set] + ".bms";

                File.WriteAllText(bmsDirectoryPath + @"\" + tempBmsFilename, bmsText1 + bmsText2 + bmsText3);

                batchText += "\"" + bmx2wavExecutablePath + "\" -c \"#temp_bmx2wav.ini\" \"" + tempBmsFilename + "\" \""+ targetWavFilename + "\"\r\n";
            }

            if (checkBox_removeTemp.IsChecked == true)
            {
                batchText += "\r\nDEL \"#temp_*\"\r\n";  // DELは危険なのでは？
            }

            batchText += "\r\nPAUSE\r\n";

            File.WriteAllText(bmsDirectoryPath + @"\" + "#temp_#batch_(click_here).bat", batchText);

            //******** 設定ファイルのコピー ********
            var targetIniFilename = bmsDirectoryPath + @"\" + "#temp_bmx2wav.ini";
            if (!File.Exists(targetIniFilename)) {
                File.Copy(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "#temp_bmx2wav.ini"), targetIniFilename, false);
            }

            //******** あとはバッチファイルを実行するだけ！ ********

            // todo:
            //   不要なwav定義の削除（任意）
            //   画面設定の記憶
        }

        private void textBox_bmx2wavPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var path = textBox_bmx2wavPath.Text;

                if (path == "")
                {
                    textBlock_bmx2wavStatus.Text = "-";
                    textBlock_bmx2wavStatus.Foreground = Brushes.Black;
                }
                else if (File.Exists(path) && Path.GetFileName(path) == "bmx2wavc.exe")
                {
                    textBlock_bmx2wavStatus.Text = "o";
                    textBlock_bmx2wavStatus.Foreground = Brushes.Green;
                }
                else
                {
                    textBlock_bmx2wavStatus.Text = "x";
                    textBlock_bmx2wavStatus.Foreground = Brushes.Red;
                }
            }
            catch
            {
            }
        }
    }
}
