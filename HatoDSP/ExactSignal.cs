using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class ExactSignal : Signal
    {
        internal readonly float[] array;  // 不変型という設定なので、内容を変更してはならない
        internal readonly float scale;

        internal ExactSignal(float[] buffer, float scale, bool copyRequired)
        {
            if (copyRequired)
            {
                buffer = buffer.ToArray();
            }
            this.array = buffer;
            this.scale = scale;
        }

        public ExactSignal(float[] array)
        {
            this.array = array.ToArray();  // オーバーヘッド＾＾
            this.scale = 1.0f;
        }

        public override float[] ToArray()
        {
            var ret = array.ToArray();  // Signal.ToArrayはオーバーヘッドが高いという認識でよろしいでしょうか

            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] *= scale;
            }

            return ret;
        }

        public override int Count
        {
            get { return array.Length; }
        }
    }
}
