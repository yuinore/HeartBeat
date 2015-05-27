using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoLib
{
    public class BitUnpacker
    {
        byte[] bitlist;
        float[] floats;
        int bitscount;
        int floatscount;
        int intCursor;
        int floatCursor;

        public BitUnpacker(float[] floatlist)
        {
            if (floatlist.Length < 3) throw new Exception("BitUnpacker: 不正な入力です。");

            int floatChunkStart = (int)(floatlist[0] + 0.5f);
            bitscount = (int)(floatlist[1] + 0.5f);

            if (floatlist.Length < floatChunkStart + 2) throw new Exception("BitUnpacker: 不正な入力です。");
            floatscount = (int)(floatlist[floatChunkStart + 1] + 0.5f);

            bitlist = new byte[(bitscount + 28) / 29 * 29];
            floats = new float[floatscount];

            for (int i = 0; i < (bitscount + 28) / 29; i++)
            {
                FloatToBits(floatlist[i + 2], bitlist, i * 29);
            }

            for (int i = 0; i < floatscount; i++)
            {
                floats[i] = floatlist[floatChunkStart + 2 + i];
            }
        }

        public long ReadInteger(int bits)
        {
            long ret = 0;
            int shift = 0;

            for (int i = 0; i < bits; i++)
            {
                ret |= ((long)bitlist[intCursor]) << shift++;

                intCursor++;
            }

            return ret;
        }

        public long ReadLinkedInteger(int bits)
        {
            long ret = 0;
            int shift = 0;

            while (true)
            {
                for (int i = 0; i < bits - 1; i++)
                {
                    ret |= (long)bitlist[intCursor] << shift++;

                    intCursor++;
                }

                if (bitlist[intCursor++] == 0) break;
            }

            return ret;
        }

        public float ReadFloat()
        {
            return floats[floatCursor++];
        }

        public string ReadString()
        {
            StringBuilder builder = new StringBuilder();

            while (true)
            {
                long isWord = ReadInteger(1);

                if (isWord == 0)
                {
                    long chcode = ReadLinkedInteger(8);
                    if (chcode == 0) break;

                    builder.Append(new string(new char[] { Convert.ToChar(chcode) }));  // ←ちょっと煩わしくない？
                }
                else
                {
                    long wordAt = ReadLinkedInteger(8);
                    builder.Append(BitPackingDictionary.Words[wordAt]);
                }
            }

            return builder.ToString();
        }

        private void FloatToBits(float f, byte[] bits, int index)
        {
            byte[] bytes = BitConverter.GetBytes(f);

            bits[index + 28] = (byte)((bytes[3] >> 7) & 1);
            bits[index + 27] = (byte)((bytes[3] >> 5) & 1);
            bits[index + 26] = (byte)((bytes[3] >> 4) & 1);
            bits[index + 25] = (byte)((bytes[3] >> 3) & 1);
            bits[index + 24] = (byte)((bytes[3] >> 2) & 1);
            bits[index + 23] = (byte)((bytes[3] >> 1) & 1);
            bits[index + 22] = (byte)((bytes[3] >> 0) & 1);

            bits[index + 21] = (byte)((bytes[2] >> 7) & 1);
            bits[index + 20] = (byte)((bytes[2] >> 6) & 1);
            bits[index + 19] = (byte)((bytes[2] >> 5) & 1);
            bits[index + 18] = (byte)((bytes[2] >> 4) & 1);
            bits[index + 17] = (byte)((bytes[2] >> 3) & 1);
            bits[index + 16] = (byte)((bytes[2] >> 2) & 1);
            bits[index + 15] = (byte)((bytes[2] >> 1) & 1);
            bits[index + 14] = (byte)((bytes[2] >> 0) & 1);

            bits[index + 13] = (byte)((bytes[1] >> 7) & 1);
            bits[index + 12] = (byte)((bytes[1] >> 6) & 1);
            bits[index + 11] = (byte)((bytes[1] >> 5) & 1);
            bits[index + 10] = (byte)((bytes[1] >> 4) & 1);
            bits[index + 9] = (byte)((bytes[1] >> 3) & 1);
            bits[index + 8] = (byte)((bytes[1] >> 2) & 1);
            bits[index + 7] = (byte)((bytes[1] >> 1) & 1);
            bits[index + 6] = (byte)((bytes[1] >> 0) & 1);

            bits[index + 5] = (byte)((bytes[0] >> 7) & 1);
            bits[index + 4] = (byte)((bytes[0] >> 6) & 1);
            bits[index + 3] = (byte)((bytes[0] >> 5) & 1);
            bits[index + 2] = (byte)((bytes[0] >> 4) & 1);
            bits[index + 1] = (byte)((bytes[0] >> 3) & 1);
            bits[index + 0] = (byte)((bytes[0] >> 2) & 1);
        }
    }
}
