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
        public int ReleasePolyphony = 4;

        int pitchBend = 0;
        int lastPitchBend = 0;
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
        public Signal[] Take(int count, float[][] audioInput = null, float[][] micInput = null)
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

                Signal pitchSig = null;

                if (pitchBend == lastPitchBend)
                {
                    pitchSig = new ConstantSignal((float)(note.n + pitchBend * bendrange / 8192.0), count);
                }
                else
                {
                    // 一度にtakeする数によって結果が異なる疑惑
                    float[] pitch = new float[count];
                    int i = 0;
                    int imax = Math.Min(256, count);

                    for (; i < imax; i++)
                    {
                        double pb2 = ((imax - i) * (double)lastPitchBend + i * (double)pitchBend) / (double)imax;
                        pitch[i] = (float)(note.n + pb2 * bendrange / 8192.0);
                    }
                    for (; i < count; i++)
                    {
                        pitch[i] = (float)(note.n + pitchBend * bendrange / 8192.0);
                    }
                    pitchSig = new ExactSignal(pitch, 1.0f, false);
                }
                lastPitchBend = pitchBend;

                var lenv = new LocalEnvironment()
                {
                    Buffer = ret,
                    Freq = new ConstantSignal(0, count),
                    Pitch = pitchSig,
                    Locals = new Dictionary<string, Signal>(),
                    Gate = new ConstantSignal(1, count),  // ここが違う！！
                    SamplingRate = 44100
                };

                if (audioInput != null)
                {
                    if (audioInput.Length == 1)
                    {
                        var sig = new ExactSignal(audioInput[0], 1.0f, false);
                        lenv.Locals.Add("audioInputL".ToLower(), sig);
                        lenv.Locals.Add("audioInputR".ToLower(), sig);
                    }
                    else if (audioInput.Length >= 2)
                    {
                        lenv.Locals.Add("audioInputL".ToLower(), new ExactSignal(audioInput[0], 1.0f, false));
                        lenv.Locals.Add("audioInputR".ToLower(), new ExactSignal(audioInput[1], 1.0f, false));
                    }
                }

                if (micInput != null)
                {
                    // TODO: JavaScriptから入力のチャンネル数を取得できるように・・・

                    if (micInput.Length == 1)
                    {
                        var sig = new ExactSignal(micInput[0], 1.0f, false);
                        lenv.Locals.Add("micInput".ToLower(), sig);
                    }
                    else if (micInput.Length >= 2)
                    {
                        lenv.Locals.Add("micInput".ToLower(), new ExactSignal(micInput[0], 1.0f, false));
                    }
                }

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

            lock (notes)
            {
                if (notes.Count >= Polyphony)
                {
                    NoteOff(notes[0].n);
                }

                var cell = rootTree.Generate();

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
                    if (ReleasePolyphony >= 1)
                    {
                        if (releasedNotes.Count >= ReleasePolyphony)
                        {
                            releasedNotes.RemoveAt(0);
                        }
                        releasedNotes.Add(item);
                    }
                    else
                    {
                        // 鍵盤を離したらすぐに発音をやめる、極端な場合。
                    }

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
