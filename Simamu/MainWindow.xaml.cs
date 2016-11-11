using HatoBMSLib;
using HatoLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            var bms2 = new BMSStruct(textBox_mergingBMS.Text, true);
            
            bms2.Export(textBox_mergingBMS.Text + "_comverted.bms");

            MessageBox.Show(this, bms2.Message);

            return;

            //----------------------------------------
            //----------------------------------------
            //----------------------------------------
            //----------------------------------------
            //----------------------------------------
            //----------------------------------------
            //----------------------------------------
            //----------------------------------------

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
            var KeySoundGroupListOrderByMeasure = MeasureToObjectSet.OrderBy(x => x.Key).Where(x => x.Value.Count >= 2).ToArray();

            //******** グループに対してUnique操作を行い、各セットに対して命名する。WAV定義リストを追加する。 ********
            HashSet<HashSet<int>> KeySoundList = new HashSet<HashSet<int>>();
            Dictionary<HashSet<int>, string> KeySoundGroupToWavFilename = new Dictionary<HashSet<int>, string>();
            Dictionary<HashSet<int>, int> KeySoundGroupToWavid = new Dictionary<HashSet<int>, int>();

            int VacantWavid = 1 + bms.WavDefinitionList.Select(x => x.Key).Where(x => bms.LNObj == null || x != bms.LNObj).Max();

            int KeyGroupIndex = 0; // FIXME:

            foreach (var kvpair in KeySoundGroupListOrderByMeasure)
            {
                var MergedKeySoundList = kvpair.Value.Select(ev => ev.Wavid).ToArray();

                if (MergedKeySoundList.Length <= 1)
                {
                    NewSoundBMObjects.Add(kvpair.Value.First());  // 同時発音数が1のキー音
                    continue;
                }

                var MergedKeySoundSet = new HashSet<int>(MergedKeySoundList);

                if (!KeySoundList.Contains(MergedKeySoundSet))
                {
                    KeySoundList.Add(MergedKeySoundSet);

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
            bms.SoundBMObjects = NewSoundBMObjects;

            bms.Export("________bms.bms");

            //******** 各グループのキー音を統合し、音声処理をする ********
            foreach (var a in KeySoundList)
            {
                Console.WriteLine(String.Join(",", a.Select(x =>
                {
                    try
                    {
                        return bms.WavDefinitionList[x];
                    }
                    catch
                    {
                        return "???";
                    }
                })));
            }
        }
    }
}
