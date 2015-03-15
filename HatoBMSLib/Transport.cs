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

        private SortedDictionary<double, double> beatToTempoChange;  // 拍数 → テンポ; 同時刻に複数のテンポチェンジは入らない
        private SortedDictionary<double, double> secondsToTempoChange;  // 拍数 → テンポ; 同時刻に複数のテンポチェンジは入らない
        // ↓キーが0の要素を必ず持つ
        // そんなことより、これがprivateじゃないのはダメだと思うんですが・・・
        internal SortedDictionary<Rational, double> measureToTempoChange;  // 小節番号 → テンポ; 同時刻に複数のテンポチェンジは入らない
        private SortedDictionary<int, double> measureToSignature;  // 小節番号 → 拍数
        private SortedDictionary<Rational, double> measureToStop;
        private SortedDictionary<double, double> beatToSpeedRate;

        private bool arranged;

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

        public Transport()
        {
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
            }
        }

        public void AddSignature(int measure, double measurelength)
        {
            if (arranged) throw new InvalidOperationException("既にTransportの値が固定されています。新たにデータを追加する場合は、Arrangedをfalseに設定して下さい。");

            measureToSignature.Add(measure, measurelength);
        }

        public void AddStopSequence(Rational measure, double stopbeats)
        {
            if (arranged) throw new InvalidOperationException("既にTransportの値が固定されています。新たにデータを追加する場合は、Arrangedをfalseに設定して下さい。");

            measureToStop.Add(measure, stopbeats);
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

            foreach (var x in measureToTempoChange)
            {
                beatToTempoChange.Add(MeasureToBeat(x.Key), x.Value);
            }
            foreach (var x in beatToTempoChange)
            {
                secondsToTempoChange.Add(BeatToSeconds(x.Key), x.Value);
            }
            foreach (var x in measureToStop)
            {
                beatToSpeedRate.Add(MeasureToBeat(x.Key), 0.0);
                beatToSpeedRate.Add(MeasureToBeat(x.Key) + x.Value, 1.0);  // ここに1.0ではない値が入る可能性もある
            }
        }

        public double MeasureToBeat(Rational measure)
        {
            int lastmeasure = 0;
            double? lastsig = null;
            double beatelapsed = 0;

            var finalmeasure = measureToSignature.LastOrDefault().Key;  // えっ・・・これO(1)で取得出来るんですかね・・・
            // 見つからない場合は0となる。

            // 停止によって遅れた時間の長さ(拍)
            double stopbeats = measureToStop.Where(x => x.Key < measure).Select(x => x.Value).Sum();

            for (int m = 0; m <= finalmeasure + 1; m++ )
            {
                double x_Value;

                if (!measureToSignature.TryGetValue(m, out x_Value))
                {
                    x_Value = 1.0;
                }

                lastsig = lastsig ?? x_Value;

                if ((double)measure <= m)
                {
                    return ((double)measure - lastmeasure) * 4 * (double)lastsig + beatelapsed + stopbeats;
                }

                beatelapsed += 4 * (double)lastsig;
                lastmeasure = m;
                lastsig = x_Value;
            }

            return ((double)measure - lastmeasure) * 4 + beatelapsed + stopbeats;
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
    }
}
