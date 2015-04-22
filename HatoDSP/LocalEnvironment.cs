using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class LocalEnvironment  // 不変型にしようと思っているのですが(ry
    {
        /// <summary>
        /// Cell で生成した信号を格納するバッファ。
        /// 元の値に += して結果を返すこと。
        /// </summary>
        public float[][] Buffer;  // Buffer[0].Length > count となる場合もあるので注意
        public Signal Pitch;
        public Signal Freq;
        public Signal Gate;
        public Dictionary<string, Signal> Locals;  // キーはすべて小文字で格納してください。これは、JavaScript上の制約です。
        public float SamplingRate;
    }
}
