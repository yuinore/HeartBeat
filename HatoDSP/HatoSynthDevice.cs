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

        class MyNoteEvent
        {
            public Cell cell;
            public int n;
        }

        // 空のパッチでシンセサイザーを初期化します。
        public HatoSynthDevice()
        {
            Task.Run(() => HatoDSPFast.FastMath.Saw(1.0, 2));
        }

        public HatoSynthDevice(string patch)
        {
            Task.Run(() => HatoDSPFast.FastMath.Saw(1.0, 2));

            var r = new PatchReader(patch);

            rootTree = r.Root;
        }

        /// <summary>
        /// 合成された2チャンネルの信号を返します。
        /// このクラスはスレッドセーフですが、同時に2つのスレッドから Take を呼ばないで下さい。
        /// </summary>
        public Signal[] Take(int count)
        {
            var mix = (new int[2]).Select(x => (Signal)(new ConstantSignal(0, count))).ToArray();

            MyNoteEvent[] notes1 = null, notes2 = null;

            lock (notes)
            {
                notes1 = notes.ToArray();
                notes2 = releasedNotes.ToArray();
            }

            foreach (var note in notes1)
            {
                var lenv = new LocalEnvironment()
                {
                    Freq = new ConstantSignal(0, count),
                    Pitch = new ConstantSignal(note.n, count),
                    Locals = new Dictionary<string, Signal>(),
                    Gate = new ConstantSignal(1, count),  // ここが違う！！
                    SamplingRate = 44100
                };
                var ret = note.cell.Take(count, lenv);
                if (ret.Length == 1)
                {
                    mix[0] = Signal.Add(mix[0], ret[0]);
                    mix[1] = Signal.Add(mix[1], ret[0]);
                }
                else if (ret.Length == 2)
                {
                    mix[0] = Signal.Add(mix[0], ret[0]);
                    mix[1] = Signal.Add(mix[1], ret[1]);
                }
                else
                {
                    throw new NotImplementedException("todo");
                }
            }

            foreach (var note in notes2)
            {
                var lenv = new LocalEnvironment()
                {
                    Freq = new ConstantSignal(0, count),
                    Pitch = new ConstantSignal(note.n, count),
                    Locals = new Dictionary<string, Signal>(),
                    Gate = new ConstantSignal(0, count),  // ここが違う！！
                    SamplingRate = 44100
                };
                var ret = note.cell.Take(count, lenv);
                if (ret.Length == 1)
                {
                    mix[0] = Signal.Add(mix[0], ret[0]);
                    mix[1] = Signal.Add(mix[1], ret[0]);
                }
                else if (ret.Length == 2)
                {
                    mix[0] = Signal.Add(mix[0], ret[0]);
                    mix[1] = Signal.Add(mix[1], ret[1]);
                }
                else
                {
                    throw new NotImplementedException("todo");
                }
            }

            return mix;
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
    }
}
