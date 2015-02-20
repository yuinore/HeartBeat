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
        internal SortedDictionary<Rational, double> measureToTempoChange;  // 小節番号 → テンポ; 同時刻に複数のテンポチェンジは入らない
        internal SortedDictionary<int, double> measureToSignature;  // 小節番号 → 拍数

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
        }

        public void AddTempoChange(Rational measure, double tempo)
        {
            if (arranged) throw new InvalidOperationException("既にTransportの値が固定されています。新たにデータを追加する場合は、Arrangedをfalseに設定して下さい。");

            measureToTempoChange.Add(measure, tempo);
        }

        public void AddSignature(int measure, double measurelength)
        {
            if (arranged) throw new InvalidOperationException("既にTransportの値が固定されています。新たにデータを追加する場合は、Arrangedをfalseに設定して下さい。");

            measureToSignature.Add(measure, measurelength);
        }

        public void ArrangeTransport()
        {
            // スレッドセーフではない
            arranged = true;

            beatToTempoChange = new SortedDictionary<double, double>();
            secondsToTempoChange = new SortedDictionary<double, double>();

            foreach (var x in measureToTempoChange)
            {
                beatToTempoChange.Add(MeasureToBeat(x.Key), x.Value);
            }
            foreach (var x in beatToTempoChange)
            {
                secondsToTempoChange.Add(BeatToSeconds(x.Key), x.Value);
            }
        }

        public double MeasureToBeat(Rational measure)
        {
            int lastmeasure = 0;
            double? lastsig = null;
            double beatelapsed = 0;

            var finalmeasure = measureToSignature.LastOrDefault().Key;  // えっ・・・これO(1)で取得出来るんですかね・・・
            // 見つからない場合は0となる。

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
                    return ((double)measure - lastmeasure) * 4 * (double)lastsig + beatelapsed;
                }

                beatelapsed += 4 * (double)lastsig;
                lastmeasure = m;
                lastsig = x_Value;
            }

            return ((double)measure - lastmeasure) * 4 + beatelapsed;
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
