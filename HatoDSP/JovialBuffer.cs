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
        /// バッファを作成し、0で初期化します。
        /// 必要に応じて、配列を再確保します。
        /// このバッファは、次にGetReferenceが呼ばれるまで有効です。
        /// </summary>
        public float[][] GetReference(int channelCount, int sampleCount)
        {
            if (buf.Length < channelCount)
            {
                float[][] buf2 = new float[channelCount][];

                int ch = 0;
                for (; ch < buf.Length; ch++)
                {
                    buf2[ch] = InitArray(buf[ch], sampleCount);
                }
                for (; ch < channelCount; ch++)
                {
                    buf2[ch] = new float[sampleCount];
                }

                buf = buf2;

                return buf2;
            }
            else
            {
                for (int ch = 0; ch < channelCount; ch++)
                {
                    buf[ch] = InitArray(buf[ch], sampleCount);
                }

                return buf;
            }
        }

        private float[] InitArray(float[] arr, int sampleCount)
        {
            if (arr.Length < sampleCount)
            {
                return new float[sampleCount];
            }

            for (int i = 0; i < sampleCount; i++)
            {
                arr[i] = 0;
            }

            return arr;
        }
    }
}
