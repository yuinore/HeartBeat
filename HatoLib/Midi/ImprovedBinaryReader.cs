using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HatoLib.Midi
{
    // 2013/06/19
    // Expand BinaryReader
    // C#で継承使ったの初めてかもしれない・・・

    /// <summary>
    /// midiやflpの読み込みに使用するメソッドを提供します。
    /// このクラスはBinaryReaderを継承します。
    /// </summary>
    internal class ImprovedBinaryReader : BinaryReader//, IDisposable
    {
        public ImprovedBinaryReader(Stream stream_)
            : base(stream_)
        {
        }

        /// <summary>
        /// midiファイルで使用されるデルタタイムを読み込みます。
        /// </summary>
        public int ReadDeltaTime()
        {
            byte ret;
            int retS = 0;
            while (true)
            {
                ret = base.ReadByte();
                retS = (retS << 7) + (ret & 0x7F);
                if ((ret & 0x80) == 0) break;
            }
            return retS;
        }

        /// <summary>
        /// flpファイルで使用されるデルタタイムを読み込みます。
        /// </summary>
        public int ReadDeltaTimeBigEndian()
        {
            byte ret;
            int retS = 0;
            int shiftbit = 0;
            while (true)
            {
                ret = base.ReadByte();
                retS = retS + ((ret & 0x7F) << shiftbit);
                shiftbit += 7;
                if ((ret & 0x80) == 0) break;
            }
            return retS;
        }

        /// <summary>
        /// Int32 を ビッグエンディアン で読み込みます。
        /// </summary>
        /// <returns></returns>
        public int ReadBigInt32()
        {
            uint ret;
            ret = base.ReadUInt32();
            return (int)(((ret & (0xFF000000u)) >> 24) + ((ret & 0x00FF0000u) >> 8) + ((ret & 0x0000FF00u) << 8) + ((ret & 0x000000FFu) << 24));
        }
    }
}
