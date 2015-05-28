using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoLib
{
    public class BitPacker
    {
        private class BitSegment
        {
            public bool IsFloat;

            public int Bits;  // Linkedに使う場合は、フラグの1ビット分を合わせたビット数
            public bool IsLinked;
            public long IntValue;

            public float FloatValue;

            public List<byte> IntegerToBits()
            {
                List<byte> bits = new List<byte>();

                if (IsLinked)
                {
                    long n = IntValue;

                    while (true)
                    {
                        for (int i = 0; i < (Bits - 1); i++)
                        {
                            bits.Add((byte)(n & 1));
                            n >>= 1;
                        }

                        bits.Add((n == 0) ? (byte)0 : (byte)1);

                        if (n == 0) break;
                    }
                }
                else
                {
                    long n = IntValue;

                    for (int i = 0; i < Bits; i++)
                    {
                        bits.Add((byte)(n & 1));
                        n >>= 1;
                    }

                    Debug.Assert(n == 0);
                }

                return bits;
            }
        }

        List<BitSegment> segments;

        public BitPacker()
        {
            segments = new List<BitSegment>();
        }

        public void AddInteger(int bits, long value)
        {
            var seg = new BitSegment();

            seg.IsFloat = false;
            seg.IsLinked = false;

            if (bits > 63 || bits < 0) throw new Exception("bitsの値が不正です。");
            seg.Bits = bits;

            if (value >= (1L << bits) || value < 0) throw new Exception("valueの値が不正です。");
            seg.IntValue = value;

            segments.Add(seg);
        }

        public void AddLinkedInteger(int bits, long value)
        {
            var seg = new BitSegment();

            if (value < 0) throw new Exception("valueの値が不正です。");

            seg.IsFloat = false;
            seg.IsLinked = true;

            if (bits > 63 || bits < 0) throw new Exception("bitsの値が不正です。");
            seg.Bits = bits;

            seg.IntValue = value;

            segments.Add(seg);
        }

        public void AddFloat(float value)
        {
            var seg = new BitSegment();

            seg.IsFloat = true;
            seg.FloatValue = value;

            segments.Add(seg);
        }

        public void AddString(string str)
        {
            for (int i = 0; i < str.Length; )
            {
                char ch = str[i];
                string substr = str.Substring(i);
                string word = null;
                int wordAt = 0;

                for (int j = 0; j < BitPackingDictionary.Words.Length; j++)
                {
                    if (substr.StartsWith(BitPackingDictionary.Words[j]))
                    {
                        if (word == null || BitPackingDictionary.Words[j].Length > word.Length)
                        {
                            word = BitPackingDictionary.Words[j];
                            wordAt = j;
                        }
                    }
                }

                if (word == null)
                {
                    long chcode = Convert.ToInt64(ch);
                    //Console.WriteLine("character:" + ch + ", code:" + chcode);

                    Debug.Assert(chcode != 0, "ヌル文字はエンコードできません。");

                    if (chcode == 0) chcode = 0x20;

                    AddInteger(1, 0);
                    AddLinkedInteger(8, chcode);

                    i++;
                }
                else
                {
                    AddInteger(1, 1);
                    AddLinkedInteger(8, wordAt);

                    i += word.Length;
                }
            }

            AddInteger(1, 0);
            AddLinkedInteger(8, 0);
        }

        public float[] ToFloatList()
        {
            List<byte> bits = new List<byte>();
            List<float> floats = new List<float>();

            foreach (var seg in segments)
            {
                if (seg.IsFloat)
                {
                    floats.Add(seg.FloatValue);
                }
                else
                {
                    bits.AddRange(seg.IntegerToBits());
                }
            }

            //********
            List<float> ret = new List<float>();

            int floatChunkStart = (bits.Count + 28) / 29 + 1;
            int bitscount = bits.Count;
            ret.Add((float)floatChunkStart);  // floatsチャンクの開始位置 - 1
            ret.Add(bitscount);  // bitsの要素数(ビット単位)

            while (bits.Count % 29 != 0)
            {
                bits.Add(0);
            }

            for (int i = 0; i < bits.Count; i += 29)
            {
                ret.Add(BitsToFloat(bits, i));
            }

            Debug.Assert(ret.Count == floatChunkStart + 1);

            ret.Add(floats.Count);  // floatsの要素数
            ret.AddRange(floats);

            return ret.ToArray();
        }

        private float BitsToFloat(List<byte> bits, int index)
        {
            // index から 29 個 のビットをfloatにエンコードします。
            byte[] bytes = new byte[4];
            bytes[3] = (byte)(
                (bits[index + 28] << 7) |
                ((bits[index + 27] ^ 1) << 6) |
                (bits[index + 27] << 5) |
                (bits[index + 26] << 4) |
                (bits[index + 25] << 3) |
                (bits[index + 24] << 2) |
                (bits[index + 23] << 1) |
                (bits[index + 22] << 0));
            bytes[2] = (byte)(
                (bits[index + 21] << 7) |
                (bits[index + 20] << 6) |
                (bits[index + 19] << 5) |
                (bits[index + 18] << 4) |
                (bits[index + 17] << 3) |
                (bits[index + 16] << 2) |
                (bits[index + 15] << 1) |
                (bits[index + 14] << 0));
            bytes[1] = (byte)(
                (bits[index + 13] << 7) |
                (bits[index + 12] << 6) |
                (bits[index + 11] << 5) |
                (bits[index + 10] << 4) |
                (bits[index + 9] << 3) |
                (bits[index + 8] << 2) |
                (bits[index + 7] << 1) |
                (bits[index + 6] << 0));
            bytes[0] = (byte)(
                (bits[index + 5] << 7) |
                (bits[index + 4] << 6) |
                (bits[index + 3] << 5) |
                (bits[index + 2] << 4) |
                (bits[index + 1] << 3) |
                (bits[index + 0] << 2) |
                (1 << 1) |
                (0 << 0));

            return BitConverter.ToSingle(bytes, 0);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var seg in segments)
            {
                if (seg.IsFloat)
                {
                    builder.Append("float: " + seg.FloatValue + "\r\n");
                }
                else
                {
                    if (seg.IsLinked)
                    {
                        if (32 <= seg.IntValue && seg.IntValue <= 126 && seg.Bits == 8)
                        {
                            builder.Append("linked[" + seg.Bits + "]: " + seg.IntValue + " '" + Convert.ToChar(seg.IntValue) +"'\r\n");
                        }
                        else
                        {
                            builder.Append("linked[" + seg.Bits + "]: " + seg.IntValue + "\r\n");
                        }
                    }
                    else
                    {
                        builder.Append("fixed[" + seg.Bits + "]: " + seg.IntValue + "\r\n");
                    }
                }
            }

            return builder.ToString();
        }
    }
}
