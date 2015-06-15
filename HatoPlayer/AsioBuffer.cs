using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoPlayer
{
    /// <summary>
    /// AsioHandlerへオーディオデータを返すためのバッファーを示します。
    /// デリゲートを作るのが面倒だったが、デリゲートを作らないと変数名を明示できなかったため、こうなった。
    /// </summary>
    public struct AsioBuffer
    {
        public readonly float[][] Buffer;
        public readonly int ChannelCount;
        public readonly int SampleCount;

        public AsioBuffer(float[][] buffer, int ch, int count)
        {
            Buffer = buffer;
            ChannelCount = ch;
            SampleCount = count;
        }
    }
}
