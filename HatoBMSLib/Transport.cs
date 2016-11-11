using HatoLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoBMSLib
{
    public class Transport
    {
        // こいつらへのアクセスは基本的には禁止したいです。
        // あっ、これをBMSStructの内部クラスにすればいいんじゃね！？！？！？
        // sortedDictionaryのメモリ使用量ってどんなもんなんでしょうかね（実際List<T>で十分な気も）

        private SortedDictionary<double, double> beatToTempoChange;  // 拍数 → テンポ; 同時刻に複数のテンポチェンジは入らない
        private SortedDictionary<double, double> secondsToTempoChange;  // 拍数 → テンポ; 同時刻に複数のテンポチェンジは入らない
        // ↓キーが0の要素を必ず持つ
        // そんなことより、これがprivateじゃないのはダメだと思うんですが・・・
        internal SortedDictionary<Rational, double> measureToTempoChange;  // 小節番号 → テンポ; 同時刻に複数のテンポチェンジは入らない
        private SortedDictionary<int, double> measureToSignature;  // 小節番号 → 拍数
        private SortedDictionary<Rational, double> measureToStop;
        private SortedDictionary<double, double> beatToSpeedRate;
        private SortedDictionary<double, Rational> beatToMeasureVertices;

        private bool arranged;

        private BMSExceptionHandler ExceptionHandler;

        /// <summary>
        /// BeatToSeconds等の関数呼び出しが準備できていることを表します。
        /// </summary>
        public bool Arranged
        {
            get
            {
                return arranged;
            }
            set
            {
                if (value == true && arranged == false)
                {
                    throw new ArgumentException("Arrangedをtrueに設定することはできません。代わりにArrangeTransport()を呼び出して下さい。");
                }
                arranged = value;
            }
        }

        public Transport(BMSExceptionHandler ExceptionHandler)
        {
            this.ExceptionHandler = ExceptionHandler;

            measureToTempoChange = new SortedDictionary<Rational, double>();
            measureToSignature = new SortedDictionary<int, double>();
            measureToStop = new SortedDictionary<Rational, double>();
        }

        public void AddTempoChange(Rational measure, double tempo)
        {
            if (arranged) throw new InvalidOperationException("既にTransportの値が固定されています。新たにデータを追加する場合は、Arrangedをfalseに設定して下さい。");

            measureToTempoChange.Add(measure, tempo);
        }

        public void AddDefaultTempo(double tempo)
        {
            if (!measureToTempoChange.ContainsKey(0))
            {
                AddTempoChange(0, tempo);
            }
            else if (Math.Abs(measureToTempoChange[0] - tempo) >= 0.0001)
            {
                // エラー
                throw new Exception("ぽえ");
            }
        }

        public void AddSignature(int measure, double measurelength)
        {
            if (arranged) throw new InvalidOperationException("既にTransportの値が固定されています。新たにデータを追加する場合は、Arrangedをfalseに設定して下さい。");

            if (measureToSignature.ContainsKey(measure))
            {
                ExceptionHandler.ThrowFormatWarning("小節長が複数回定義されています。");
            }
            measureToSignature[measure] = measurelength;
        }

        public void AddStopSequence(Rational measure, double stopbeats)
        {
            if (arranged) throw new InvalidOperationException("既にTransportの値が固定されています。新たにデータを追加する場合は、Arrangedをfalseに設定して下さい。");

            // stopbeatsが拍数であることに注意
            
            if (measureToStop.ContainsKey(measure))
            {
                ExceptionHandler.ThrowFormatWarning("ストップシーケンスが複数回定義されています。");
            }
            measureToStop[measure] = stopbeats;
        }

        public void ArrangeTransport()
        {
            // スレッドセーフではない
            arranged = true;

            if (!measureToTempoChange.ContainsKey(0))
            {
                throw new InvalidOperationException("Transportにテンポの初期値が設定されていません。");
            }

            beatToTempoChange = new SortedDictionary<double, double>();
            secondsToTempoChange = new SortedDictionary<double, double>();
            beatToSpeedRate = new SortedDictionary<double, double>();
            beatToMeasureVertices = new SortedDictionary<double, Rational>();

            foreach (var x in measureToTempoChange)
            {
                beatToTempoChange.Add(MeasureToBeat(x.Key), x.Value);
            }

            // 以降では BeatToSeconds(), MeasureToSeconds() が使用可能

            foreach (var x in beatToTempoChange)
            {
                secondsToTempoChange.Add(BeatToSeconds(x.Key), x.Value);
            }

            // 以降では SecondsToBeat() が使用可能

            var finalmeasure = measureToSignature.LastOrDefault().Key;  // defaultで0
            for (int m = 0; m <= finalmeasure + 2; m++)
            {
                beatToMeasureVertices[MeasureToBeat(m)] = m;
            }

            foreach (var x in measureToStop)
            {
                beatToSpeedRate.Add(MeasureToBeat(x.Key), 0.0);
                beatToSpeedRate.Add(MeasureToBeat(x.Key) + x.Value, 1.0);  // ここに1.0ではない値が入る可能性もある

                beatToMeasureVertices[MeasureToBeat(x.Key)] = x.Key;
                beatToMeasureVertices[MeasureToBeat(x.Key) + x.Value] = x.Key;
            }

            // 以降では BeatToDisplacement(), BeatToMeasure() が使用可能
        }

        public double MeasureToBeat(Rational measure)
        {
            int lastmeasure = -1;
            double? lastsig = null;
            double beatelapsed = 0;

            var finalmeasure = measureToSignature.LastOrDefault().Key;  // えっ・・・これO(1)で取得出来るんですかね・・・
            // 見つからない場合は0となる。

            // 停止によって遅れた時間の長さ(拍)
            double stopbeats = measureToStop.Where(x => x.Key < measure).Select(x => x.Value).Sum();

            for (int m = 0; m <= finalmeasure + 1; m++)
            {
                double measureLen;

                if (!measureToSignature.TryGetValue(m, out measureLen))
                {
                    measureLen = 1.0;
                }

                lastsig = lastsig ?? 0;  // -1小節目の小節長は0（とするか、lastmeasureの初期値を -1、beatelapsedの初期値を -4 としてもよい。）

                if ((double)measure <= m)
                {
                    return ((double)measure - lastmeasure) * 4 * (double)lastsig + beatelapsed + stopbeats;
                }

                beatelapsed += 4 * (double)lastsig;
                lastmeasure = m;
                lastsig = measureLen;
            }

            return ((double)measure - lastmeasure) * 4 + beatelapsed + stopbeats;
        }

        public Rational BeatToMeasure(double beat)
        {
            if (!arranged) throw new InvalidOperationException("先にArrangeTransport関数を呼び出して下さい。");

            var y = default(KeyValuePair<double, Rational>);
            var z = default(KeyValuePair<double, Rational>);
            bool init = false;

            foreach (var x in beatToMeasureVertices)
            {
                if (!init)
                {
                    init = true;  // ループの1回目はスキップ（lastbeatとlasttempoは設定する）
                }
                else
                {
                    if (beat <= x.Key)
                    {
                        return Rational.FromDouble((beat - y.Key) / (x.Key - y.Key)) * (x.Value - y.Value) + y.Value;  // いつも使ってる線形補間の式だね！
                    }
                }

                z = y;
                y = x;
            }

            return Rational.FromDouble((beat - z.Key) / (y.Key - z.Key)) * (y.Value - z.Value) + z.Value;
        }

        public double BeatToSeconds(double beat)
        {
            if (!arranged) throw new InvalidOperationException("先にArrangeTransport関数を呼び出して下さい。");

            double lastbeat = 0;
            double? lasttempo = null;
            double timeelapsed = 0;

            foreach (var x in beatToTempoChange)
            {
                lasttempo = lasttempo ?? x.Value;

                if (beat <= x.Key)
                {
                    return (beat - lastbeat) * 60.0 / (double)lasttempo + timeelapsed;
                }

                timeelapsed += (x.Key - lastbeat) * 60.0 / (double)lasttempo;
                lastbeat = x.Key;
                lasttempo = x.Value;
            }

            return (beat - lastbeat) * 60.0 / (double)lasttempo + timeelapsed;
        }

        public double BeatToDisplacement(double beat)
        {
            if (!arranged) throw new InvalidOperationException("先にArrangeTransport関数を呼び出して下さい。");

            double lastbeat = 0;
            double? lasttempo = null;
            double timeelapsed = 0;

            foreach (var x in beatToSpeedRate)
            {
                lasttempo = lasttempo ?? 1.0;

                if (beat <= x.Key)
                {
                    return (beat - lastbeat) * (double)lasttempo + timeelapsed;
                }

                timeelapsed += (x.Key - lastbeat) * (double)lasttempo;
                lastbeat = x.Key;
                lasttempo = x.Value;
            }

            // beatToSpeedRateは必ずしも要素を含んでいる必要は無いからね、仕方ないね
            return (beat - lastbeat) * (lasttempo ?? 1.0) + timeelapsed;
        }

        public double SecondsToBeat(double seconds)
        {
            if (!arranged) throw new InvalidOperationException("先にArrangeTransport関数を呼び出して下さい。");

            double lastseconds = 0;
            double? lasttempo = null;
            double beatelapsed = 0;

            foreach (var x in secondsToTempoChange)
            {
                lasttempo = lasttempo ?? x.Value;

                if (seconds <= x.Key)
                {
                    return (seconds - lastseconds) * (double)lasttempo / 60.0 + beatelapsed;
                }

                beatelapsed += (x.Key - lastseconds) * (double)lasttempo / 60.0;
                lastseconds = x.Key;
                lasttempo = x.Value;
            }
            
            return (seconds - lastseconds) * (double)lasttempo / 60.0 + beatelapsed;
        }

        public double MeasureToSeconds(Rational measure)
        {
            if (!arranged) throw new InvalidOperationException("先にArrangeTransport関数を呼び出して下さい。");

            return BeatToSeconds(MeasureToBeat(measure));
        }

        public void Export(out List<BMObject> objs, out SortedDictionary<int, double> BPMDefinitionList, out SortedDictionary<int, double> StopDefinitionList)
        {
            // 次の要素をBMSに書き出す
            // measureToTempoChange
            // measureToSignature
            // measureToStop

            const int BMSCH_TEMPO = 3;
            const int BMSCH_EXTEMPO = 8;
            const int BMSCH_SIGNATURE = 2;
            const int BMSCH_STOP = 9;

            int BPMDefinitionCursor = 1;  // 一番最初のデータが入っていない定義番号
            int StopDefinitionCursor = 1;  // 一番最初のデータが入っていない定義番号

            const double BPM_PRECISION = 0.00001;
            const double STOP_PRECISION = 1.0;
            
            objs = new List<BMObject>();
            BPMDefinitionList = new SortedDictionary<int, double>();
            StopDefinitionList = new SortedDictionary<int, double>();

            foreach (var ev in measureToTempoChange)
            {
                if (ev.Value == (long)ev.Value && 1 <= ev.Value && ev.Value <= 255)
                {
                    objs.Add(new BMObject(BMSCH_TEMPO, 0, BMConvert.FromBase36(String.Format("{0:X2}", ev.Value)), ev.Key));
                }
                else
                {
                    double roundedValue = Math.Round(ev.Value / BPM_PRECISION) * BPM_PRECISION;

                    if (roundedValue <= 0) roundedValue = BPM_PRECISION;

                    int defId = -1;

                    if (BPMDefinitionList.ContainsValue(ev.Value))  // FIXME: 遅そう
                    {
                        defId = BPMDefinitionList.First(x => x.Value == ev.Value).Key;  // FIXME: 遅そう
                    }
                    else
                    {
                        BPMDefinitionList[BPMDefinitionCursor] = ev.Value;
                        defId = BPMDefinitionCursor;

                        BPMDefinitionCursor++;  // この数字、登場順ではなく数値順なのでは・・・？
                    }

                    objs.Add(new BMObject(BMSCH_EXTEMPO, 0, defId, ev.Key));
                }
            }

            foreach (var ev in measureToSignature)
            {
                objs.Add(new BMObjectSignature(ev.Value, ev.Key));
            }

            foreach (var ev in measureToStop)
            {
                double roundedValue = Math.Round(ev.Value * 48.0 / STOP_PRECISION) * STOP_PRECISION;

                if (roundedValue <= 0) roundedValue = STOP_PRECISION;

                int defId = -1;

                if (StopDefinitionList.ContainsValue(ev.Value))  // FIXME: 遅そう
                {
                    defId = StopDefinitionList.First(x => x.Value == ev.Value).Key;  // FIXME: 遅そう
                }
                else
                {
                    StopDefinitionList[StopDefinitionCursor] = ev.Value;
                    defId = StopDefinitionCursor;

                    StopDefinitionCursor++;
                }

                objs.Add(new BMObject(BMSCH_STOP, 0, defId, ev.Key));
            }
        }
    }
}
