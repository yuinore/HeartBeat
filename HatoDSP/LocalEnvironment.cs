using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class LocalEnvironment : ICloneable  // 不変型ではないですが、そのせいで色々と危ないコードになってる
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

        public LocalEnvironment Clone()
        {
            Dictionary<string, Signal> loc = new Dictionary<string,Signal>();

            foreach(var entry in Locals) {  // ←ここがオーバーヘッドになるかも・・・？？
                loc.Add(entry.Key, entry.Value);  // Signalは不変型
            }

            return new LocalEnvironment()
            {
                Buffer = Buffer,
                Pitch = Pitch,
                Freq = Freq,
                Gate = Gate,
                Locals = loc,
                SamplingRate = SamplingRate
            };
        }

        // ↓明示的なインターフェイスの実装
        object ICloneable.Clone()
        {
            return this.Clone();
        }
    }
}
