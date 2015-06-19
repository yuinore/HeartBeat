using HatoDSPFast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    static class SlowMath
    {
        /// <summary>
        /// デシベルを振幅比に変換します。
        /// </summary>
        public static float DecibelToRaw(float dB)
        {
            if (dB <= -99.95f) return 0;  // ？？？

            return (float)Math.Pow(10, dB * 0.05);
        }

        /// <summary>
        /// 正の振幅比(gain > 0)をデシベルに変換します。
        /// </summary>
        public static float RawToDecibel(float gain)
        {
            if (gain <= 0.00001f) return -100.0f;  // ？？？

            return 20.0f * (float)Math.Log10(gain);
        }

        /// <summary>
        /// 1から0に減衰する関数を返します。
        /// tは0～1の範囲です。
        /// </summary>
        public static float Envelope(float t)
        {
            // 半ハン窓と、-40dBの指数減衰の積。
            return (float)(Math.Pow(0.01, t) * (0.5 + 0.5 * Math.Cos(Math.PI * t)));
        }

        /// <summary>
        /// ノート番号（実数）を基本周波数[Hz]に変換します。
        /// </summary>
        public static float PitchToFreq(float pitch)
        {
            return (float)(FastMathWrap.Pow2((pitch - 69.0) / 12.0) * 441);
        }
    }
}
