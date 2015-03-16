using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HatoLib
{
    /// <summary>
    /// 参考資料
    /// http://en.wikipedia.org/wiki/Ogg
    /// http://www.xiph.org/vorbis/doc/framing.html
    /// </summary>
    public class VorbisFileReader
    {
        public VorbisFileReader(Stream strm)
        {
            throw new NotImplementedException();
        }

        public static float[][] ReadAllSamples(string filename)
        {
            float[] buf;
            byte[] bbuf; 
            int ch;
            int bitdepth;

            // TODO: ファイルを２回読み込むのをやめる
            long totalsamples = GetTotalSamples(filename);


            try
            {
                using (var vreader = new Tsukikage.Audio.OggDecodeStream(new FileStream(filename, FileMode.Open, FileAccess.Read)))
                {
                    ch = vreader.Channels;
                    buf = new float[totalsamples * ch];
                    bbuf = new byte[vreader.Length];
                    bitdepth = vreader.BitsPerSample;

                    if (vreader.Length > Int32.MaxValue)
                    {
                        throw new Exception("ogg重すぎるよ・・・");
                    }
                    vreader.Read(bbuf, 0, (int)vreader.Length);
                }
                // ↓謎の変換作業 1↓

                for (int n = 0; n < buf.Length; n++)
                {
                    switch (bitdepth)
                    {
                        case 16:
                            buf[n] = BitConverter.ToInt16(bbuf, n * bitdepth / 8) / 65536.0f;
                            //buf[n] = BitConverter.ToInt16(new byte[] { bbuf[n * 2 + 1], bbuf[n * 2] }, 0) / 65536.0f;
                            break;
                        case 24:
                            throw new Exception("未　実　装");
                            //buf[n] = Convert.ToInt32(new byte[] { bbuf[n * 2], bbuf[n * 2 + 1] });
                            //break;
                        case 32:
                            buf[n] = BitConverter.ToSingle(bbuf, n * bitdepth / 8);
                            break;
                        default:
                            throw new Exception("あー16bitでも24bitでも32bitでもないoggを読み込もうとしちゃったんだねー");
                    }
                }

                // ↓謎の変換作業 2↓

                float[][] buf2 = new float[ch][];

                for (int i = 0; i < ch; i++)
                {
                    buf2[i] = new float[buf.Length / ch];
                }

                for (int i = 0; i < buf.Length; i++)
                {
                    buf2[i % ch][i / ch] = buf[i];
                }

                return buf2;
            }
            catch
            {
                return new float[][] { new float[] { 0 } };  // ひどすぎる
            }
        }

        /// <summary>
        /// 全サンプル数を64ビット符号付き整数で返します。
        /// </summary>
        public static long GetTotalSamples(String filename)
        {
            byte[] buf;
            // TODO: 引数にStreamを渡せるようにする
            buf = System.IO.File.ReadAllBytes(filename);  // 例外が起きるかもしれないね
            // なんかもうめんどくさいから全部読み込んでいいよね(クズ
            int i = 0;  // reading point(byte)

            while (buf[i] == 'O' && buf[i + 1] == 'g' && buf[i + 2] == 'g' && buf[i + 3] == 'S' && buf[i + 4] == '\0')
            {
                if ((buf[i + 5] & 4) == 4)  // [header_type_flag] last page of logical bitstream (eos)
                {
                    // end
                    long len = 0L;
                    len += ((long)buf[i + 6 + 0] << 0);
                    len += ((long)buf[i + 6 + 1] << 8);
                    len += ((long)buf[i + 6 + 2] << 16);
                    len += ((long)buf[i + 6 + 3] << 24);
                    len += ((long)buf[i + 6 + 4] << 32);
                    len += ((long)buf[i + 6 + 5] << 40);
                    len += ((long)buf[i + 6 + 6] << 48);
                    len += ((long)buf[i + 6 + 7] << 56);
                    return len;
                }
                // not end

                i += 0x1A;
                int page_segments = (int)buf[i++];  // [page_segments]
                int segment_table__sum = 0;
                while ((page_segments--) != 0)
                {
                    segment_table__sum += buf[i++];
                }
                i += segment_table__sum;  // skip to next "OggS"
            }
            throw new Exception("oggファイルが不正です");
        }

        /// <summary>
        /// サンプリングレートをかえします。
        /// </summary>
        public static int GetSamplingRate(String filename)
        {
            BinaryReader br = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read));

            byte[] buf = br.ReadBytes(0x28);
            if (buf[0] == 'O' && buf[1] == 'g' && buf[2] == 'g' && buf[3] == 'S' && buf[4] == '\0')
            {
                return br.ReadInt32();
            }
            throw new Exception("oggファイルが不正です");
        }
    }
}
