﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class IIRFilter
    {
        readonly int chCnt;

        float inv_a0, a1, a2;
        float b0, b1, b2;
        float[] z0, z1, z2;

        public IIRFilter(int chCnt, float a0, float a1, float a2, float b0, float b1, float b2)
        {
            this.chCnt = chCnt;

            z0 = new float[chCnt];
            z1 = new float[chCnt];
            z2 = new float[chCnt];

            UpdateParams(a0, a1, a2, b0, b1, b2);
        }

        public void UpdateParams(float a0, float a1, float a2, float b0, float b1, float b2)
        {
            float inv_a0_ = 1.0f / a0;

            this.inv_a0 = 1.0f;
            this.a1 = a1 * inv_a0_;
            this.a2 = a2 * inv_a0_;
            this.b0 = b0 * inv_a0_;
            this.b1 = b1 * inv_a0_;
            this.b2 = b2 * inv_a0_;
        }

        // input[0] : フィルタへの入力信号
        // input[1] : a,bパラメータ
        public void Take(int count, float[][][] input)
        {
            if (input.Length != 1 && input.Length != 2) throw new Exception("Invalid Input Count.");
            if (input[0].Length != chCnt) throw new Exception("Invalid Input Signal's Channels Count.");
            float[][] param = null;
            if (input.Length >= 2)
            {
                param = input[1];
            }

            for (int j = 0; j < chCnt; j++)
            {
                //if (input[0][j].Length != count) throw new Exception("Invalid Input Signal's Length.");

                float t0 = z0[j];  // これで高速化はされるのか？ → 計測したら高速化されてるっぽいです・・・
                float t1 = z1[j];
                float t2 = z2[j];

                for (int i = 0; i < count; i++)
                {
                    if (input.Length >= 2)
                    {
                        inv_a0 = 1.0f / param[0][i];
                        a1 = param[1][i];
                        a2 = param[2][i];
                        b0 = param[3][i];
                        b1 = param[4][i];
                        b2 = param[5][i];
                    }

                    t0 = input[0][j][i] - (a1 * t1 + a2 * t2) * inv_a0;
                    if (-1.1754944e-38 < t0){  // 別に分岐が遅いわけではなく上下の計算が遅いのかも（って思ったら上の方で除算をしていることに気付いた）
                        if (t0 < 1.1754944e-38)
                        {
                            t0 = 0;
                        }
                    } // 1.1754944e-38 は 2^(-126) で、正の最小の正規化数
                    input[0][j][i] = (t0 * b0 + t1 * b1 + t2 * b2) * inv_a0;

                    t2 = t1;
                    t1 = t0;
                }

                z0[j] = t0;
                z1[j] = t1;
                z2[j] = t2;
            }
        }
    }
}
