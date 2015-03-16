using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoLib
{
    public class AudioFileReader
    {

        public static bool TryToReadAllSamples(string filename, out float[][] buf)
        {
            buf = null;

            if (Path.GetExtension(filename) == ".wav")
            {
                if (File.Exists(filename)) { buf = ReadAllSamplesWav(filename); return true; }
                else if (File.Exists(Path.ChangeExtension(filename, ".ogg"))) { buf = ReadAllSamplesVorbis(Path.ChangeExtension(filename, ".ogg")); return true; }
            }
            else if (Path.GetExtension(filename) == ".ogg")
            {
                if (File.Exists(filename)) { buf = ReadAllSamplesVorbis(filename); return true; }
                else if (File.Exists(Path.ChangeExtension(filename, ".wav"))) { buf = ReadAllSamplesWav(Path.ChangeExtension(filename, ".wav")); return true; }
            }
            else
            {
                if (File.Exists(Path.ChangeExtension(filename, ".ogg"))) { buf = ReadAllSamplesVorbis(Path.ChangeExtension(filename, ".ogg")); return true; }
                else if (File.Exists(Path.ChangeExtension(filename, ".wav"))) { buf = ReadAllSamplesWav(Path.ChangeExtension(filename, ".wav")); return true; }
            }

            return false;
        }

        public static float[][] ReadAllSamples(string filename)
        {
            float[][] buf;

            if (TryToReadAllSamples(filename, out buf))
            {
                return buf;
            }
            else
            {
                throw new FileNotFoundException("ファイル " + filename + " が見つかりませんでした");
            }
        }

        private static float[][] ReadAllSamplesWav(string filename)
        {
            return WaveFileReader.ReadAllSamples(new FileStream(filename, FileMode.Open, FileAccess.Read));
        }

        private static float[][] ReadAllSamplesVorbis(string filename)
        {
            return VorbisFileReader.ReadAllSamples(filename);
        }


        public static void ReadAttribute(string filename, out int SamplingRate, out int ChannelsCount, out int BufSamplesCount)
        {
            if (Path.GetExtension(filename) == ".wav")
            {
                if (File.Exists(filename)) { ReadAttributeWav(filename, out  SamplingRate, out  ChannelsCount, out  BufSamplesCount); return; }
                else if (File.Exists(Path.ChangeExtension(filename, ".ogg"))) { ReadAttributeVorbis(Path.ChangeExtension(filename, ".ogg"), out  SamplingRate, out  ChannelsCount, out BufSamplesCount); return; }
            }
            else if (Path.GetExtension(filename) == ".ogg")
            {
                if (File.Exists(filename)) { ReadAttributeVorbis(filename, out  SamplingRate, out  ChannelsCount, out BufSamplesCount); return; }
                else if (File.Exists(Path.ChangeExtension(filename, ".wav"))) { ReadAttributeWav(Path.ChangeExtension(filename, ".wav"), out  SamplingRate, out  ChannelsCount, out  BufSamplesCount); return; }
            }
            else
            {
                if (File.Exists(Path.ChangeExtension(filename, ".ogg"))) { ReadAttributeVorbis(Path.ChangeExtension(filename, ".ogg"), out  SamplingRate, out  ChannelsCount, out BufSamplesCount); return; }
                else if (File.Exists(Path.ChangeExtension(filename, ".wav"))) { ReadAttributeWav(Path.ChangeExtension(filename, ".wav"), out  SamplingRate, out  ChannelsCount, out  BufSamplesCount); return; }
            }

            throw new FileNotFoundException("ファイル " + filename + " が見つかりませんでした");
        }

        private static void ReadAttributeWav(string filename, out int SamplingRate, out int ChannelsCount, out int BufSamplesCount)
        {
            using (var wreader = new WaveFileReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
            {
                SamplingRate = wreader.SamplingRate;
                BufSamplesCount = (int)wreader.SamplesCount;
                ChannelsCount = wreader.ChannelsCount;
            }
        }

        private static void ReadAttributeVorbis(string filename, out int SamplingRate, out int ChannelsCount, out int BufSamplesCount)
        {
            using (var wreader = new Tsukikage.Audio.OggDecodeStream(new FileStream(filename, FileMode.Open, FileAccess.Read)))
            {
                SamplingRate = wreader.SamplesPerSecond;
                BufSamplesCount = (int)(wreader.Length * 8L / wreader.BitsPerSample);
                ChannelsCount = wreader.Channels;
            }
        }


        public static bool FileExists(string filename)
        {
            return File.Exists(filename)
                || File.Exists(Path.ChangeExtension(filename, ".wav"))
                || File.Exists(Path.ChangeExtension(filename, ".ogg"));
        }

        public static string FileName(string filename)
        {
            if (File.Exists(filename))
            {
                return filename;
            }
            else if (File.Exists(Path.ChangeExtension(filename, ".wav")))
            {
                return Path.ChangeExtension(filename, ".wav");
            }
            else if (File.Exists(Path.ChangeExtension(filename, ".ogg")))
            {
                return Path.ChangeExtension(filename, ".ogg");
            }

            return null;
        }
    }
}