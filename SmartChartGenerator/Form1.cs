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
                textBox1.Text = filenames[0];

                Analyze();
            }
        }

        private void chart1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void Analyze()
        {
            string filename = textBox1.Text;

            if (!File.Exists(filename)) return;

            BMSStruct b = new BMSStruct(filename);

            //MessageBox.Show(b.PlayableBMObjects.Count + "notes");

            // 集計
            #region オブジェ数計算
            Dictionary<int, int> SecondToNotesCount = new Dictionary<int, int>();
            Dictionary<int, int> SecondToScratchCount = new Dictionary<int, int>();
            int maxSec = 10;
            int maxNotes = 5;
            {
                foreach (var obj in b.PlayableBMObjects)
                {
                    int sec = (int)Math.Floor(obj.Seconds);
                    SecondToNotesCount[sec] = SecondToNotesCount.GetValueOrDefault(sec) + 1;
                    SecondToScratchCount[sec] = SecondToScratchCount.GetValueOrDefault(sec) + (obj.Keyid == 6 ? 1 : 0);

                    maxSec = Math.Max(sec, maxSec);
                    maxNotes = Math.Max(maxNotes, SecondToNotesCount[sec]);
                }
            }
            #endregion

            #region オブジェ密度計算
            Dictionary<int, double> RegionToMaxDensity = new Dictionary<int, double>();
            Dictionary<int, double> RegionToMaxScratchDensity = new Dictionary<int, double>();
            double regionWidthSeconds = 1;
            double averagingWidthSeconds = 3.0;  // seconds

            Func<IEnumerable<BMObject>, Dictionary<int, double>> GetDensityDictionary = (objs) =>
            {
                if (objs.Count() == 0) return new Dictionary<int, double>();

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

                    Console.WriteLine("max density : " + (maxObjCount / averagingWidthSeconds) + " at " + maxDensityAt);
                }

                Dictionary<int, double> dict = new Dictionary<int, double>();

                foreach (var kvpair in SecondToDensity)
                {
                    int secondInteger = (int)Math.Floor(kvpair.Key / regionWidthSeconds);
                    dict[secondInteger] = Math.Max(kvpair.Value, dict.GetValueOrDefault(secondInteger));
                }

                return dict;
            };

            RegionToMaxDensity = GetDensityDictionary(b.PlayableBMObjects);
            RegionToMaxScratchDensity = GetDensityDictionary(b.PlayableBMObjects.Where(x => x.Keyid == 6));


            #endregion

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
            chart1.ChartAreas[0].AxisY.Maximum = maxNotes / 5 * 5 + 10;
            chart1.ChartAreas[0].AxisY.Interval = 5;
            
            // グラフのテスト
            chart1.Series.Clear();
            chart1.Legends.Clear();
            
            Series test4 = new Series();
            {
                test4.ChartType = SeriesChartType.StackedColumn;
                test4.Color = Color.FromArgb(0xFF, 0x33, 0x66);
                test4.Name = "Scratch Count";

                for (int i = 0; i < maxSec + 2 + 5; i++)
                {
                    //if (SecondToScratchCount.GetValueOrDefault(i) != 0)
                    //{
                    test4.Points.AddXY(i, SecondToScratchCount.GetValueOrDefault(i));
                    //}
                }

                chart1.Series.Add(test4);
            }

            Series test = new Series();
            {
                test.ChartType = SeriesChartType.StackedColumn;
                test.Color = Color.FromArgb(0x99, 0xCC, 0x66);
                test.Name = "Notes Count";

                for (int i = 0; i < maxSec + 2 + 5; i++)
                {
                    test.Points.AddXY(i, SecondToNotesCount.GetValueOrDefault(i) - SecondToScratchCount.GetValueOrDefault(i));
                }

                chart1.Series.Add(test);
            }

            Series test2 = new Series();
            {
                test2.ChartType = SeriesChartType.Line;
                test2.Color = Color.FromArgb(0x00, 0x66, 0xCC);
                test2.BorderWidth = 2;
                test2.Name = "Moving Ave (3sec)";

                for (int i = 0; i < (maxSec + averagingWidthSeconds + 1) / regionWidthSeconds; i++)
                {
                    test2.Points.AddXY((double)i * regionWidthSeconds, RegionToMaxDensity.GetValueOrDefault(i) + 0.0001);
                }

                chart1.Series.Add(test2);
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
    }
}
