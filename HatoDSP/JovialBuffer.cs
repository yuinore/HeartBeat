using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    class JovialBuffer
    {
        float[][] buf;

        public JovialBuffer()
        {
            buf = new float[1][] { new float[256] };
        }

        /// <summary>
        /// バッファを作成し、initialValueで初期化します。
        /// 必要に応じて、配列を再確保します。
        /// initialValueが0のときは、少し高速に処理をすることができます。
        /// このバッファは、次にGetReferenceが呼ばれるまで有効です。
        /// sampleCountを1ずつ増やしながらGetReferenceを呼んだりすると困ってしまいます。
        /// </summary>
        public float[][] GetReference(int channelCount, int sampleCount, float initialValue = 0.0f)
        {
            if (buf.Length < channelCount)
            {
                float[][] buf2 = new float[channelCount][];

                int ch = 0;
                for (; ch < buf.Length; ch++)
                {
                    buf2[ch] = InitArray(buf[ch], sampleCount, initialValue);
                }
                for (; ch < channelCount; ch++)
                {
                    buf2[ch] = new float[sampleCount];

                    if (initialValue != 0.0f)
                    {
                        InitArray(buf2[ch], sampleCount, initialValue);
                    }
                }

                buf = buf2;

                return buf2;
            }
            else
            {
                for (int ch = 0; ch < channelCount; ch++)
                {
                    buf[ch] = InitArray(buf[ch], sampleCount, initialValue);
                }

                return buf;
            }
        }

        private float[] InitArray(float[] arr, int sampleCount, float initialValue)
        {
            if (arr.Length < sampleCount)
            {
                var arr2 = new float[sampleCount];

                if (initialValue != 0.0f)
                {
                    for (int i = 0; i < sampleCount; i++)
                    {
                        arr2[i] = initialValue;
                    }
                }

                return arr2;
            }

            for (int i = 0; i < sampleCount; i++)
            {
                arr[i] = initialValue;
            }

            return arr;
        }
    }
}
