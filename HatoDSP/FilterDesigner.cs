using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HatoDSP
{
    static class FilterDesigner
    {
        // 先生！２次のフィルタを縦続接続するのが正しい実装方法だと思います！！
        // その方が誤差も蓄積されにくいんだってね！

        // アナログフィルタからデジタルフィルタを設計する方法はこれ
        // 双一次変換 - 和歌山大学
        // http://www.wakayama-u.ac.jp/~kawahara/signalproc/TeXfiles/bilinearTrans.pdf

        /// <summary>
        /// バターワースフィルタを設計して、係数行列を返します。
        /// 返り値を r とすると、差分方程式は次のように表されます：
        /// r[i][0]y[n] + r[i][1]y[n-1] + r[i][2]y[n-2] = r[i][3]x[n] + r[i][4]x[n-1] + r[i][5]x[n-2] = r
        /// </summary>
        /// <param name="degree"></param>
        /// <param name="_2pi_normalized_cutoff"></param>
        /// <returns></returns>
        public static float[][] Butterworth(int degree, double _2pi_normalized_cutoff)
        {
            // 返り値
            List<float[]> coef = new List<float[]>();

            // アナログフィルタをデジタルフィルタに対応させる双一次変換という操作によって、
            // カットオフ周波数が変化してしまうため、
            // その補正（プリワーピング）を行う。
            // この値が、アナログフィルタ設計に使用するカットオフ周波数となる。
            // （正確には角周波数）
            float cutof = (float)Math.Tan(_2pi_normalized_cutoff / 2);

            if (degree % 2 == 0)
            {
                // ここでは何もしない
            }
            else
            {
                // 分母が "s+1" 、分子が 1 で表される2次(1次？？) の全極型アナログフィルタを追加
                // ただしこれはカットオフ周波数が2piで正規化された場合なので、それを考慮すると、これは
                // s を s / cutof で置き換える必要がある。

                // 次に、先ほどのフィルタに s = (1 - zinv) / (1 + zinv) を代入したものを離散フィルタの伝達関数とする。
                // これは有理関数になる。

                // 最後に、係数をcoefに追加する。分母を先に追加する。

                // 以上より
                coef.Add(new float[] {
                    1 - cutof,  cutof - 1,  0,  // a (差分方程式におけるyの係数, 伝達関数H(z)の分母)
                    cutof,      cutof,      0   // b (差分方程式におけるxの係数, 伝達関数H(z)の分子)
                });
            }

            for (int k = 1; k <= degree / 2; k++)
            {
                float keisuu = (float)(-2 * Math.Cos((2 * k + degree - 1) * Math.PI / (2 * degree)));
                // 分母が "1 + keisuu * s + s^2" 、分子が 1 で表される全極型アナログフィルタを追加
                // また、これはカットオフ周波数が2piで正規化された場合なので、
                // s を s / cutof で置き換える必要がある。

                // 次に、先ほどのフィルタに s = (1 - zinv) / (1 + zinv) を代入したものを離散フィルタの伝達関数とする。
                // これは有理関数になる。

                // 最後に、係数をcoefに追加する。分母を先に追加する。

                // 以上より
                float ww = cutof * cutof;

                // 後で消すメモ: b1 に -1 を掛けるとハイパスフィルタ
                float a0 = ww + keisuu * cutof + 1;  // 分母の定数項
                float a1 = 2 * (cutof - 1);          // 分母の z^-1 の係数
                float a2 = ww - keisuu * cutof + 1;  // 分母の z^-2 の係数
                float b0 = ww;                       // 分子の定数項
                float b1 = 2 * ww;                   // 分子の z^-1 の係数
                float b2 = ww;                       // 分子の z^-2 の係数

                coef.Add(new[] { a0, a1, a2, b0, b1, b2 });
            }

            return coef.ToArray();
        }

        public static void Biquad(
            FilterType type, float w0, float Q, float A,
            out float a0, out float a1, out float a2, out float b0, out float b1, out float b2)
        {
            // w0 ... 2pi normalized frequency
            // Q ... Quality parameter

            float sin = (float)Math.Sin(w0);
            float cos = (float)Math.Cos(w0);
            float alp = sin / Q;
            float sqrtA = 0;

            float[] ab = null;

            System.Diagnostics.Debug.Assert(A >= 0);

            switch (type)
            {
                case FilterType.LowPass:
                    ab = new float[6] { 1 + alp, -2 * cos, 1 - alp, (1 - cos) * 0.5f, +(1 - cos), (1 - cos) * 0.5f };
                    break;
                case FilterType.HighPass:
                    ab = new float[6] { 1 + alp, -2 * cos, 1 - alp, (1 + cos) * 0.5f, -(1 + cos), (1 + cos) * 0.5f };
                    break;
                case FilterType.BandPass:
                    ab = new float[6] { 1 + alp, -2 * cos, 1 - alp, sin * 0.5f, 0, -sin * 0.5f };
                    // *NOTICE: constant skirt gain, peak gain = Q
                    break;
                case FilterType.Notch:
                    ab = new float[6] { 1 + alp, -2 * cos, 1 - alp, 1, -2 * cos, 1 };
                    break;
                case FilterType.AllPass:
                    ab = new float[6] { 1 + alp, -2 * cos, 1 - alp, 1 - alp, -2 * cos, 1 + alp };
                    break;
                case FilterType.Peaking:
                    ab = new float[6] { 1 + alp / A, -2 * cos, 1 - alp / A, 1 + alp * A, -2 * cos, 1 - alp * A };
                    break;
                case FilterType.LowShelf:
                    sqrtA = (float)Math.Sqrt(A);
                    ab = new float[6] {
                        (A+1) + (A-1)*cos + 2*sqrtA*alp,
                        -2*( (A-1) + (A+1)*cos),
                        (A+1) + (A-1)*cos - 2*sqrtA*alp,
                        A*( (A+1) - (A-1)*cos + 2*sqrtA*alp ),
                        2*A*( (A-1) - (A+1)*cos ),
                        A*( (A+1) - (A-1)*cos - 2*sqrtA*alp )
                    };
                    break;
                case FilterType.HighShelf:
                    sqrtA = (float)Math.Sqrt(A);
                    ab = new float[6] {
                        (A+1) - (A-1)*cos + 2*sqrtA*alp,
                        2*( (A-1) - (A+1)*cos),
                        (A+1) - (A-1)*cos - 2*sqrtA*alp,
                        A*( (A+1) + (A-1)*cos + 2*sqrtA*alp ),
                        -2*A*( (A-1) + (A+1)*cos ),
                        A*( (A+1) + (A-1)*cos - 2*sqrtA*alp )
                    };
                    break;
                default:
                    ab = new float[6];
                    break;
            }

            a0 = ab[0];  // TODO: リファクタリング
            a1 = ab[1];
            a2 = ab[2];
            b0 = ab[3];
            b1 = ab[4];
            b2 = ab[5];
        }
    }
}
