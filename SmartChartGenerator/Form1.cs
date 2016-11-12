using HatoBMSLib;
using HatoLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Data.SQLite;

namespace SmartChartGenerator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void chart1_DragDrop(object sender, DragEventArgs e)
        {
            String[] filenames = ((string[])e.Data.GetData(DataFormats.FileDrop));

            if (filenames.Length >= 1)
            {
                if (File.Exists(filenames[0]))
                {
                    textBox1.Text = filenames[0];

                    Analyze();
                }
                else if (Directory.Exists(filenames[0]))
                {
                    List<string> flist = new List<string>();

                    foreach (var dir in Directory.GetDirectories(filenames[0]))
                    {
                        flist.AddRange(Directory.GetFiles(dir).Where(file =>
                        {
                            var ext = Path.GetExtension(file);
                            return ext == ".bms" || ext == ".bme" || ext == ".bml" || ext == ".pms";
                        }));
                    }

                    //AnalyzeFiles();
                }
            }
        }

        private void chart1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        BMSStruct b = null;
        StreamWriter sw;

        private void Analyze()
        {
            string filename = textBox1.Text;
            double TDP, MDP;

            Analyze(filename, true, -1, out TDP, out MDP);
        }

        bool AnalyzingNotesPerSecond = false;

        private void Analyze(string filename, bool drawGraph, int ExLevel, out double TDP, out double MDP)
        {
            TDP = MDP = -1;

            if (!File.Exists(filename)) return;

            if (Path.GetExtension(filename) == ".db")
            {
                AnalyzeDatabase();
                return;
            }

            AnalyzingNotesPerSecond = radioButton1.Checked;

            b = new BMSStruct(filename);

            //MessageBox.Show(b.PlayableBMObjects.Count + "notes");

            // 集計
            #region オブジェ数計算
            //__________________________________________________________________________ Short1-7 Long1-7 ShortScr LongScr
            Dictionary<int, int> SecondToNotesCount = new Dictionary<int, int>(); //____   yes      yes     yes      yes
            Dictionary<int, int> SecondToScratchCount = new Dictionary<int, int>(); //__   no       no      yes      yes
            Dictionary<int, int> SecondToLNCount = new Dictionary<int, int>(); //_______   no       yes     no       yes
            Dictionary<int, int> SecondToLNScratchCount = new Dictionary<int, int>(); //   no       no      no       yes
            int maxSec = 10;
            int maxNotes = 5;
            {
                foreach (var obj in b.PlayableBMObjects)
                {
                    int sec = 0;
                    if (AnalyzingNotesPerSecond)
                    {
                        sec = (int)Math.Floor(obj.Seconds);
                    }
                    else
                    {
                        sec = (int)Math.Floor((double)obj.Measure);
                    }
                    SecondToNotesCount[sec] = SecondToNotesCount.GetValueOrDefault(sec) + 1;
                    SecondToScratchCount[sec] = SecondToScratchCount.GetValueOrDefault(sec) + (obj.Keyid == 6 ? 1 : 0);
                    SecondToLNCount[sec] = SecondToLNCount.GetValueOrDefault(sec) + (obj.Terminal != null ? 1 : 0);
                    SecondToLNScratchCount[sec] = SecondToLNScratchCount.GetValueOrDefault(sec) + ((obj.Terminal != null && obj.Keyid == 6) ? 1 : 0);

                    maxSec = Math.Max(sec, maxSec);
                    maxNotes = Math.Max(maxNotes, SecondToNotesCount[sec]);
                }
            }
            #endregion

            #region オブジェ密度計算
            Dictionary<int, double> RegionToMaxDensity = new Dictionary<int, double>();
            Dictionary<int, double> RegionToMinDensity = new Dictionary<int, double>();
            Dictionary<int, double> RegionToMaxScratchDensity = new Dictionary<int, double>();
            Dictionary<int, double> RegionToMinScratchDensity = new Dictionary<int, double>();
            double regionWidthSeconds = 1;
            double averagingWidthSeconds = 3.0;  // seconds

            Action<IEnumerable<BMObject>, Dictionary<int, double>, Dictionary<int, double>> GetDensityDictionary = (objs, dictMax, dictMin) =>
            {
                if (objs.Count() == 0) return;

                Dictionary<double, double> SecondToDensity = new Dictionary<double, double>();
                {
                    var nSecAve = objs.Select(x => Tuple.Create(x.Seconds, 1.0)).Concat(objs.Select(x => Tuple.Create(x.Seconds + averagingWidthSeconds, -1.0))).OrderBy(x => x.Item1);

                    int duration = (int)Math.Floor(nSecAve.Last().Item1) + 1;

                    nSecAve = nSecAve.Concat(Enumerable.Range(0, (int)((duration + averagingWidthSeconds + 10) / regionWidthSeconds)).Select(x => Tuple.Create((double)x * regionWidthSeconds, 0.0))).OrderBy(x => x.Item1);

                    double maxObjCount = 0;
                    double maxDensityAt = -1;
                    double currentObjCount = 0;

                    foreach (var o in nSecAve)
                    {
                        currentObjCount += o.Item2;
                        if (currentObjCount > maxObjCount)
                        {
                            maxObjCount = currentObjCount;
                            maxDensityAt = o.Item1;
                        }

                        SecondToDensity[o.Item1] = currentObjCount / averagingWidthSeconds;
                    }

                    //Console.WriteLine("max density : " + (maxObjCount / averagingWidthSeconds) + " at " + maxDensityAt);
                }

                //dictMax = new Dictionary<int, double>();

                foreach (var kvpair in SecondToDensity)
                {
                    int secondInteger = (int)Math.Floor(kvpair.Key / regionWidthSeconds);
                    dictMax[secondInteger] = Math.Max(kvpair.Value, dictMax.GetValueOrDefault(secondInteger));
                    dictMin[secondInteger] = Math.Min(kvpair.Value - 1000, dictMin.GetValueOrDefault(secondInteger));
                }

                var tempdict = new Dictionary<int, double>();
                foreach (var kvpair in dictMin)
                {
                    tempdict[kvpair.Key] = kvpair.Value + 1000;
                }
                foreach (var kvpair in tempdict)
                {
                    dictMin[kvpair.Key] = kvpair.Value;
                }
                // ↑クソ

                //return dictMax;
            };

            GetDensityDictionary(b.PlayableBMObjects, RegionToMaxDensity, RegionToMinDensity);
            GetDensityDictionary(b.PlayableBMObjects.Where(x => x.Keyid == 6), RegionToMaxScratchDensity, RegionToMinScratchDensity);

            #endregion

            #region オブジェ密度計算
            Dictionary<int, double> RegionToTateren = new Dictionary<int, double>();
            {
                int[] keyIdToSearch = new int[] { 1, 2, 3, 4, 5, 8, 9, 6 };

                foreach (int keyid in keyIdToSearch)
                {
                    int NAssign = (keyid == 6 ? 2 : 1);  // keyidに通常アサインされるキーの数、皿のみ2

                    double lastseconds = -99999.0;

                    foreach (var obj in b.PlayableBMObjects.Where(x => x.Keyid == keyid))
                    {
                        int region = (int)Math.Floor(obj.Seconds / regionWidthSeconds);

                        if (obj.Seconds == lastseconds) throw new Exception("なんか変なので修正してください");

                        double TaterenValue = 1.0 / ((obj.Seconds - lastseconds) * NAssign); // 縦連価[Hz] ... 1 ÷ (縦連を行う時間 × アサインされたキーの数)

                        RegionToTateren[region] = Math.Max(TaterenValue, RegionToTateren.GetValueOrDefault(region));

                        lastseconds = obj.Seconds;
                    }
                }
            }
            #endregion

            if (drawGraph)
            {
                //軸ラベルの設定
                //chart1.ChartAreas[0].AxisX.Title = "angle(rad)";
                //chart1.ChartAreas[0].AxisY.Title = "sin";
                //※Axis.TitleFontでフォントも指定できるがこれはデザイナで変更したほうが楽

                //X軸最小値、最大値、目盛間隔の設定
                chart1.ChartAreas[0].AxisX.Minimum = 0;
                chart1.ChartAreas[0].AxisX.Maximum = (int)(maxSec + 5);
                chart1.ChartAreas[0].AxisX.Interval = 20;

                //Y軸最小値、最大値、目盛間隔の設定
                chart1.ChartAreas[0].AxisY.Minimum = 0;
                chart1.ChartAreas[0].AxisY.Maximum = (maxNotes * 1.25) / 5 * 5 + 5;
                chart1.ChartAreas[0].AxisY.Interval = 5;

                // グラフのテスト
                chart1.Series.Clear();
                chart1.Legends.Clear();

                Action<Chart, SeriesChartType, Color, string, int, int, Func<int, double>> AddSeries =
                    (chart, chartType, color, seriesName, start, end, f) =>
                {
                    Series series = new Series();
                    {
                        series.ChartType = chartType;
                        series.Color = color;
                        series.Name = seriesName;

                        for (int i = start; i < end; i++)
                        {
                            series.Points.AddXY(i, f(i));
                        }

                        chart1.Series.Add(series);
                    }
                };

                AddSeries(chart1,
                    SeriesChartType.StackedColumn, Color.FromArgb(0x99, 0x00, 0x33), "Long Scratch Count",
                    0, maxSec + 2 + 5,
                    i => SecondToLNScratchCount.GetValueOrDefault(i));

                AddSeries(chart1,
                    SeriesChartType.StackedColumn, Color.FromArgb(0xFF, 0x33, 0x66), "Scratch Count",
                    0, maxSec + 2 + 5,
                    i => SecondToScratchCount.GetValueOrDefault(i) - SecondToLNScratchCount.GetValueOrDefault(i));

                AddSeries(chart1,
                    SeriesChartType.StackedColumn, Color.FromArgb(0xFF, 0xCC, 0x33), "LN Count",
                    0, maxSec + 2 + 5,
                    i => SecondToLNCount.GetValueOrDefault(i) - SecondToLNScratchCount.GetValueOrDefault(i));

                AddSeries(chart1,
                    SeriesChartType.StackedColumn, Color.FromArgb(0x99, 0x99, 0x99), "Notes Count",
                    0, maxSec + 2 + 5,
                    i => SecondToNotesCount.GetValueOrDefault(i) - SecondToScratchCount.GetValueOrDefault(i)
                        - SecondToLNCount.GetValueOrDefault(i) + SecondToLNScratchCount.GetValueOrDefault(i));

                if (AnalyzingNotesPerSecond)
                {
                    /*Series test7 = new Series();
                    {
                        test7.ChartType = SeriesChartType.Line;
                        test7.Color = Color.FromArgb(0x88, 0x00, 0xFF);
                        test7.BorderWidth = 2;
                        test7.Name = "LN Count_";

                        for (int i = 0; i < maxSec + 2 + 5; i++)
                        {
                            test7.Points.AddXY(i, SecondToLNCount.GetValueOrDefault(i));
                        }

                        chart1.Series.Add(test7);
                    }*/
                    
                    Series test2 = new Series();
                    {
                        test2.ChartType = SeriesChartType.Line;
                        test2.Color = Color.FromArgb(0x00, 0x66, 0xCC);
                        test2.BorderWidth = 2;
                        test2.Name = "M Ave Max (3sec)";

                        for (int i = 0; i < (maxSec + averagingWidthSeconds + 1) / regionWidthSeconds; i++)
                        {
                            test2.Points.AddXY((double)i * regionWidthSeconds, RegionToMaxDensity.GetValueOrDefault(i));
                        }

                        chart1.Series.Add(test2);
                    }

                    Series test6 = new Series();
                    {
                        test6.ChartType = SeriesChartType.Line;
                        test6.Color = Color.FromArgb(0x00, 0x66, 0x66);
                        test6.BorderWidth = 1;
                        test6.Name = "M Ave Min (3sec)";

                        for (int i = 0; i < (maxSec + averagingWidthSeconds + 1) / regionWidthSeconds; i++)
                        {
                            test6.Points.AddXY((double)i * regionWidthSeconds, RegionToMinDensity.GetValueOrDefault(i));
                        }

                        chart1.Series.Add(test6);
                    }

                    Series test5 = new Series();
                    {
                        test5.ChartType = SeriesChartType.Line;
                        test5.Color = Color.FromArgb(0x66, 0x00, 0xCC);
                        test5.BorderWidth = 2;
                        test5.Name = "Tateren [Hz]";

                        for (int i = 0; i < (maxSec + averagingWidthSeconds + 1) / regionWidthSeconds; i++)
                        {
                            test5.Points.AddXY((double)i * regionWidthSeconds, RegionToTateren.GetValueOrDefault(i));
                        }

                        chart1.Series.Add(test5);
                    }
                }

                Legend legend = new Legend();
                legend.DockedToChartArea = chart1.ChartAreas[0].Name;
                legend.Alignment = StringAlignment.Near;
                legend.Docking = Docking.Left | Docking.Top;
                chart1.Legends.Add(legend);

                /*Series test3 = new Series();
                {
                    test3.ChartType = SeriesChartType.Line;
                    test3.Color = Color.FromArgb(0xFF, 0x00, 0x33);
                    test3.BorderWidth = 2;

                    for (int i = 0; i < (maxSec + averagingWidthSeconds + 1) / regionWidthSeconds; i++)
                    {
                        test3.Points.AddXY((double)i * regionWidthSeconds, RegionToMaxScratchDensity.GetValueOrDefault(i) + 0.0001);
                    }

                    chart1.Series.Add(test3);

                    Legend legend = new Legend();
                    legend.DockedToChartArea = "Density (Scratch)";
                    legend.Alignment = StringAlignment.Near;
                    chart1.Legends.Add(legend);
                }*/
            }

            var denslist = SecondToNotesCount.Select(x => x.Value).OrderBy(x => -x).ToList();
            if (denslist.Count < 10)
            {
                label2.Text = "???";
            }
            else
            {
                label2.Text = "#PLAYLEVEL:" + b.Playlevel + ", TDP:" + denslist[9] + ", MDP:" + denslist[0];
                TDP = denslist[9];  //**********************ここ！！！！！！！！！！！！！！！！
                MDP = denslist[0];

                if (sw != null)
                {
                    sw.WriteLine(
                        b.Title.Replace(',', '_').Replace('"', '_') + "," +
                        ExLevel + "," +
                        denslist[0] + "," + denslist[2] + "," + denslist[4] + "," + denslist[9] +","+
                        RegionToTateren.Values.Max());
                }
            }
        }

        private void chart1_MouseClick(object sender, MouseEventArgs e)
        {
        }

        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            if (b == null) return;

            double x, y;
            try
            {
                // クライアント座標をグラフ上の座標に変換する
                x = chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.X);
                y = chart1.ChartAreas[0].AxisY.PixelPositionToValue(e.Y);
            }
            catch
            {
                // 位置引数は 0～100 の範囲内で指定する必要があります。
                return;
            }
            // Chartコントロールにデータを追加する
            //chart1.Series[0].Points.AddXY(x, y);

            if (AnalyzingNotesPerSecond)
            {
                var seconds = x;

                var measure = b.transp.BeatToMeasure(b.transp.SecondsToBeat(seconds));
                var measure2 = b.transp.BeatToMeasure(b.transp.SecondsToBeat(seconds + 1));

                var integPartOfMeasure = (int)Math.Floor((double)measure);
                var integPartOfMeasure2 = (int)Math.Floor((double)measure2);

                label1.Text =
                    "#" + integPartOfMeasure.ToString("D3")// + "  " + (measure - integPartOfMeasure).ToString()
                    + " ～ #" + integPartOfMeasure2.ToString("D3");// + "  " + (measure2 - integPartOfMeasure2).ToString();
            }
            else
            {
                var measure = Math.Round(x);
                
                label1.Text = "#" + ((int)measure).ToString("D3");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Analyze();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            //ofd.InitialDirectory = "";
            ofd.Filter = "Be-Music Sequence File(*.bms;*.bme;*.bml;.pms)|*.bms;*.bme;*.bml;.pms|song.db(*.db)|*.db|すべてのファイル(*.*)|*.*";
            ofd.FilterIndex = 1;
            ofd.Title = "Open BMS File";
            ofd.RestoreDirectory = true;
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = ofd.FileName;
                
                Analyze();
            }
        }

        private void AnalyzeDatabase()
        {
            using (sw = new StreamWriter(new FileStream("insanebms_analyzed.csv", FileMode.Create, FileAccess.Write), Encoding.GetEncoding("Shift_JIS")))
            {
                sw.WriteLine("title,exlevel,1st,3rd,5th,10th,max tateren");

                double[] TDPs, MDPs;
                double[][] TDPList = new double[24][];
                double[][] MDPList = new double[24][];

                for (int lv = 1; lv <= 24; lv++)
                {
                    AnalyzeLevelFolder(true, lv, out TDPs, out MDPs);

                    TDPList[lv - 1] = TDPs;
                    MDPList[lv - 1] = MDPs;
                }

                {
                    //軸ラベルの設定
                    //chart1.ChartAreas[0].AxisX.Title = "angle(rad)";
                    //chart1.ChartAreas[0].AxisY.Title = "sin";
                    //※Axis.TitleFontでフォントも指定できるがこれはデザイナで変更したほうが楽

                    //X軸最小値、最大値、目盛間隔の設定
                    chart1.ChartAreas[0].AxisX.Minimum = 0;
                    chart1.ChartAreas[0].AxisX.Maximum = 24;
                    chart1.ChartAreas[0].AxisX.Interval = 5;

                    //Y軸最小値、最大値、目盛間隔の設定
                    chart1.ChartAreas[0].AxisY.Minimum = 0;
                    chart1.ChartAreas[0].AxisY.Maximum = 50;
                    chart1.ChartAreas[0].AxisY.Interval = 5;

                    // グラフのテスト
                    chart1.Series.Clear();
                    chart1.Legends.Clear();

                    string[] labels = new string[] { "最小値", "第一四分位点", "中央値", "第三四分位点", "最大値" };

                    for (int i = 0; i < 5; i++)
                    {
                        Series test = new Series();
                        {
                            test.ChartType = SeriesChartType.Line;
                            test.BorderWidth = 2;
                            test.Color = Color.FromArgb(0x00, 0xCC * (4 - i) / 4, 0xCC * i / 4);
                            test.Name = labels[i];

                            for (int j = 0; j < 24; j++)
                            {
                                test.Points.AddXY((double)(j + 1), TDPList[j][i]);
                            }

                            chart1.Series.Add(test);
                        }
                    }

                    Legend legend = new Legend();
                    legend.DockedToChartArea = chart1.ChartAreas[0].Name;
                    legend.Alignment = StringAlignment.Near;
                    legend.Docking = Docking.Left | Docking.Top;
                    chart1.Legends.Add(legend);

                    label2.Text = "★1～★24 Density Distribution";
                }
            }

            sw = null;
        }

        private void AnalyzeLevelFolder(bool drawGraph, int ExLevel, out double[] TDPs, out double[] MDPs)
        {
            string db_file = textBox1.Text;
            TDPs = new double[] { -1, -1, -1, -1, -1 };
            MDPs = new double[] { -1, -1, -1, -1, -1 };
            List<double> TDPList = new List<double>();
            List<double> MDPList = new List<double>();
            
            if (!File.Exists(db_file)) return;

            Dictionary<string, string> TitleToBMSPath = new Dictionary<string, string>();

            using (var conn = new SQLiteConnection("Data Source=" + db_file))
            {
                conn.Open();
                using (SQLiteCommand command = conn.CreateCommand())
                {
                    command.CommandText = "select path, title from song where exlevel == " + ExLevel + " and mode == 7";
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string path = reader["path"].ToString();
                            string title = reader["title"].ToString();
                            TitleToBMSPath[title] = path;  // duplicate elimination
                        }
                    }
                }
                conn.Close();
            }

            Dictionary<int, int> RegionToTDPCount = new Dictionary<int, int>();
            Dictionary<int, int> RegionToMDPCount = new Dictionary<int, int>();

            foreach (var kvpair in TitleToBMSPath)
            {
                double TDP, MDP;

                Analyze(kvpair.Value, false, ExLevel, out TDP, out MDP);

                RegionToTDPCount[(int)Math.Round(TDP)] = RegionToTDPCount.GetValueOrDefault((int)Math.Round(TDP)) + 1;
                RegionToMDPCount[(int)Math.Round(MDP)] = RegionToMDPCount.GetValueOrDefault((int)Math.Round(MDP)) + 1;

                if (TDP == -1 || MDP == -1)
                {
                    Console.WriteLine("Error? : " + kvpair.Value + " ( " + kvpair.Key + " )");
                }
                else
                {
                    TDPList.Add(TDP);
                    MDPList.Add(MDP);
                }
            }

            {
                TDPList.Sort();
                MDPList.Sort();
                var TDPSorted = TDPList.ToArray();
                var MDPSorted = MDPList.ToArray();
                TDPs[0] = TDPSorted.ElementAt(0 * (TDPSorted.Length - 1) / 4.0);
                TDPs[1] = TDPSorted.ElementAt(1 * (TDPSorted.Length - 1) / 10.0);
                TDPs[2] = TDPSorted.ElementAt(2 * (TDPSorted.Length - 1) / 4.0);
                TDPs[3] = TDPSorted.ElementAt(9 * (TDPSorted.Length - 1) / 10.0);
                TDPs[4] = TDPSorted.ElementAt(4 * (TDPSorted.Length - 1) / 4.0);
                MDPs[0] = MDPSorted.ElementAt(0 * (MDPSorted.Length - 1) / 4.0);
                MDPs[1] = MDPSorted.ElementAt(1 * (MDPSorted.Length - 1) / 10.0);
                MDPs[2] = MDPSorted.ElementAt(2 * (MDPSorted.Length - 1) / 4.0);
                MDPs[3] = MDPSorted.ElementAt(9 * (MDPSorted.Length - 1) / 10.0);
                MDPs[4] = MDPSorted.ElementAt(4 * (MDPSorted.Length - 1) / 4.0);
            }

            int xMax = Math.Max(49, RegionToMDPCount.Where(x => x.Value >= 1).Select(x => x.Key).Max()) + 1;
            int yMax = Math.Max(19, RegionToMDPCount.Where(x => x.Value >= 1).Select(x => x.Value).Max()) + 1;

            if (drawGraph)
            {
                //軸ラベルの設定
                //chart1.ChartAreas[0].AxisX.Title = "angle(rad)";
                //chart1.ChartAreas[0].AxisY.Title = "sin";
                //※Axis.TitleFontでフォントも指定できるがこれはデザイナで変更したほうが楽

                //X軸最小値、最大値、目盛間隔の設定
                chart1.ChartAreas[0].AxisX.Minimum = 0;
                chart1.ChartAreas[0].AxisX.Maximum = xMax;
                chart1.ChartAreas[0].AxisX.Interval = 5;

                //Y軸最小値、最大値、目盛間隔の設定
                chart1.ChartAreas[0].AxisY.Minimum = 0;
                chart1.ChartAreas[0].AxisY.Maximum = yMax;
                chart1.ChartAreas[0].AxisY.Interval = 5;

                // グラフのテスト
                chart1.Series.Clear();
                chart1.Legends.Clear();

                Series test = new Series();
                {
                    test.ChartType = SeriesChartType.Column;
                    test.Color = Color.FromArgb(0x00, 0xCC, 0x33);
                    test.Name = "TDP";

                    for (int i = 0; i <= xMax; i++)
                    {
                        test.Points.AddXY((double)i, RegionToTDPCount.GetValueOrDefault(i));
                    }

                    chart1.Series.Add(test);
                }

                Series test2 = new Series();
                {
                    test2.ChartType = SeriesChartType.Column;
                    test2.Color = Color.FromArgb(0x00, 0x66, 0xCC);
                    test2.Name = "MDP";

                    for (int i = 0; i <= xMax; i++)
                    {
                        test2.Points.AddXY((double)i, RegionToMDPCount.GetValueOrDefault(i));
                    }

                    chart1.Series.Add(test2);
                }

                Legend legend = new Legend();
                legend.DockedToChartArea = chart1.ChartAreas[0].Name;
                legend.Alignment = StringAlignment.Near;
                legend.Docking = Docking.Left | Docking.Top;
                chart1.Legends.Add(legend);

                label2.Text = "★" + ExLevel + " Density Distribution";
            }

            b = null;
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            textBox1.Text = ((string[])(string[])e.Data.GetData(DataFormats.FileDrop, false))[0];

            Analyze();
        }
    }
}
