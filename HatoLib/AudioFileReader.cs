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

            if (Path.GetExtension(filename).ToLower() == ".wav")
            {
                if (File.Exists(filename)) { buf = WaveFileReader.ReadAllSamples(filename); return true; }
                else if (File.Exists(Path.ChangeExtension(filename, ".ogg"))) { buf = VorbisFileReader.ReadAllSamples(Path.ChangeExtension(filename, ".ogg")); return true; }
            }
            else if (Path.GetExtension(filename).ToLower() == ".ogg")
            {
                if (File.Exists(filename)) { buf = VorbisFileReader.ReadAllSamples(filename); return true; }
                else if (File.Exists(Path.ChangeExtension(filename, ".wav"))) { buf = WaveFileReader.ReadAllSamples(Path.ChangeExtension(filename, ".wav")); return true; }
            }
            else
            {
                if (File.Exists(Path.ChangeExtension(filename, ".ogg"))) { buf = VorbisFileReader.ReadAllSamples(Path.ChangeExtension(filename, ".ogg")); return true; }
                else if (File.Exists(Path.ChangeExtension(filename, ".wav"))) { buf = WaveFileReader.ReadAllSamples(Path.ChangeExtension(filename, ".wav")); return true; }
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

        public static void ReadAttribute(string filename, out int SamplingRate, out int ChannelCount, out int BufSampleCount)
        {
            if (Path.GetExtension(filename).ToLower() == ".wav")
            {
                if (File.Exists(filename)) { ReadAttributeWav(filename, out  SamplingRate, out  ChannelCount, out  BufSampleCount); return; }
                else if (File.Exists(Path.ChangeExtension(filename, ".ogg"))) { ReadAttributeVorbis(Path.ChangeExtension(filename, ".ogg"), out  SamplingRate, out  ChannelCount, out BufSampleCount); return; }
            }
            else if (Path.GetExtension(filename).ToLower() == ".ogg")
            {
                if (File.Exists(filename)) { ReadAttributeVorbis(filename, out  SamplingRate, out  ChannelCount, out BufSampleCount); return; }
                else if (File.Exists(Path.ChangeExtension(filename, ".wav"))) { ReadAttributeWav(Path.ChangeExtension(filename, ".wav"), out  SamplingRate, out  ChannelCount, out  BufSampleCount); return; }
            }
            else
            {
                if (File.Exists(Path.ChangeExtension(filename, ".ogg"))) { ReadAttributeVorbis(Path.ChangeExtension(filename, ".ogg"), out  SamplingRate, out  ChannelCount, out BufSampleCount); return; }
                else if (File.Exists(Path.ChangeExtension(filename, ".wav"))) { ReadAttributeWav(Path.ChangeExtension(filename, ".wav"), out  SamplingRate, out  ChannelCount, out  BufSampleCount); return; }
            }

            throw new FileNotFoundException("ファイル " + filename + " が見つかりませんでした");
        }

        private static void ReadAttributeWav(string filename, out int SamplingRate, out int ChannelCount, out int BufSampleCount)
        {
            using (var wreader = new WaveFileReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
            {
                SamplingRate = wreader.SamplingRate;
                BufSampleCount = (int)wreader.SampleCount;
                ChannelCount = wreader.ChannelCount;
            }
        }

        private static void ReadAttributeVorbis(string filename, out int SamplingRate, out int ChannelCount, out int BufSampleCount)
        {
            using (var wreader = new NVorbis.VorbisReader(new FileStream(filename, FileMode.Open, FileAccess.Read), true))
            {
                SamplingRate = wreader.SampleRate;
                BufSampleCount = (int)wreader.TotalSamples;
                ChannelCount = wreader.Channels;
            }
        }


        public static bool FileExists(string filename)
        {
            if (filename.ToLower().StartsWith("synth:")) return false;

            return File.Exists(filename)
                || File.Exists(Path.ChangeExtension(filename, ".wav"))
                || File.Exists(Path.ChangeExtension(filename, ".ogg"));
        }

        public static string FileName(string filename)
        {
            if (filename.ToLower().StartsWith("synth:")) return null;

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