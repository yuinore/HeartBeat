using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class HatoSynthDevice
    {
        CellTree rootTree;
        List<MyNoteEvent> notes = new List<MyNoteEvent>();
        List<MyNoteEvent> releasedNotes = new List<MyNoteEvent>();

        public int Polyphony = 4;
        public int ReleasePolyphony = 2;

        int pitchBend = 0;
        int bendrange = 1;

        class MyNoteEvent
        {
            public Cell cell;
            public int n;
        }

        // 空のパッチでシンセサイザーを初期化します。
        public HatoSynthDevice()
        {
            Task.Run(() => HatoDSPFast.FastMathWrap.Saw(1.0, 2));
        }

        public HatoSynthDevice(string patch)
        {
            Task.Run(() => HatoDSPFast.FastMathWrap.Saw(1.0, 2));

            var r = new PatchReader(patch);

            rootTree = r.Root;
        }

        /// <summary>
        /// 合成された2チャンネルの信号を返します。
        /// このクラスはスレッドセーフですが、同時に2つのスレッドから Take を呼ばないで下さい。
        /// </summary>
        public Signal[] Take(int count)
        {
            var mix = (new int[2]).Select(x => new float[count]).ToArray();

            MyNoteEvent[] notes1 = null, notes2 = null;

            lock (notes)
            {
                notes1 = notes.ToArray();
                notes2 = releasedNotes.ToArray();
            }

            foreach (var note in notes1)
            {
                float[][] ret = new float[note.cell.ChannelCount][].Select(x => new float[count]).ToArray();

                var lenv = new LocalEnvironment()
                {
                    Buffer = ret,
                    Freq = new ConstantSignal(0, count),
                    Pitch = new ConstantSignal((float)(note.n + pitchBend * bendrange / 8192.0), count),
                    Locals = new Dictionary<string, Signal>(),
                    Gate = new ConstantSignal(1, count),  // ここが違う！！
                    SamplingRate = 44100
                };
                note.cell.Take(count, lenv);
                if (note.cell.ChannelCount == 1)
                {
                    for (int i = 0; i < count; i++)
                    {
                        mix[0][i] = mix[0][i] + ret[0][i];
                        mix[1][i] = mix[1][i] + ret[0][i];
                    }
                }
                else if (note.cell.ChannelCount == 2)
                {
                    for (int i = 0; i < count; i++)
                    {
                        mix[0][i] = mix[0][i] + ret[0][i];
                        mix[1][i] = mix[1][i] + ret[1][i];
                    }
                }
                else
                {
                    throw new NotImplementedException("todo");
                }
            }

            foreach (var note in notes2)
            {
                float[][] ret = new float[note.cell.ChannelCount][].Select(x => new float[count]).ToArray();

                var lenv = new LocalEnvironment()
                {
                    Buffer = ret,
                    Freq = new ConstantSignal(0, count),
                    Pitch = new ConstantSignal(note.n, count),
                    Locals = new Dictionary<string, Signal>(),
                    Gate = new ConstantSignal(0, count),  // ここが違う！！
                    SamplingRate = 44100
                };
                note.cell.Take(count, lenv);
                if (note.cell.ChannelCount == 1)
                {
                    for (int i = 0; i < count; i++)
                    {
                        mix[0][i] = mix[0][i] + ret[0][i];
                        mix[1][i] = mix[1][i] + ret[0][i];
                    }
                }
                else if (note.cell.ChannelCount == 2)
                {
                    for (int i = 0; i < count; i++)
                    {
                        mix[0][i] = mix[0][i] + ret[0][i];
                        mix[1][i] = mix[1][i] + ret[1][i];
                    }
                }
                else
                {
                    throw new NotImplementedException("todo");
                }
            }

            return mix.Select(x => new ExactSignal(x, 1.0f, false)).ToArray();
        }

        /// <summary>
        /// 音符を直ちに再生します。このクラスはスレッドセーフです。
        /// </summary>
        public void NoteOn(int n)
        {
            NoteOff(n);

            if (notes.Count >= Polyphony)
            {
                NoteOff(notes[0].n);
            }

            var cell = rootTree.Generate();

            lock (notes)
            {
                notes.Add(new MyNoteEvent()
                {
                    cell = cell,
                    n = n
                });
            }
        }

        /// <summary>
        /// 音符を直ちに停止します。このクラスはスレッドセーフです。
        /// </summary>
        public void NoteOff(int n)
        {
            lock (notes)
            {
                var list = notes.FindAll(x => x.n == n).ToArray();
                foreach (var item in list)
                {
                    if (releasedNotes.Count >= ReleasePolyphony)
                    {
                        releasedNotes.RemoveAt(0);
                    }
                    releasedNotes.Add(item);
                    notes.Remove(item);  // 遅いかも？
                }
            }
        }

        public void PitchBend(int bend)
        {
            System.Diagnostics.Debug.Assert(-8192 <= bend && bend <= 8191);

            pitchBend = bend;
        }
    }
}
