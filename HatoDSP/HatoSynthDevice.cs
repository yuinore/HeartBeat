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

        class MyNoteEvent
        {
            public Cell cell;
            public int n;
        }

        // 空のパッチでシンセサイザーを初期化します。
        public HatoSynthDevice()
        {
        }

        public HatoSynthDevice(string patch)
        {
            var r = new PatchReader(patch);

            rootTree = r.Root;
        }

        /// <summary>
        /// 合成された2チャンネルの信号を返します。
        /// </summary>
        public Signal[] Take(int count)
        {
            var mix = (new int[2]).Select(x => (Signal)(new ConstantSignal(0, count))).ToArray();

            foreach (var note in notes)
            {
                var lenv = new LocalEnvironment()
                {
                    Freq = new ConstantSignal(0, count),
                    Pitch = new ConstantSignal(note.n, count),
                    Locals = new Dictionary<string, Signal>(),
                    Gate = new ConstantSignal(1, count),
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
            foreach (var note in releasedNotes)
            {
                var lenv = new LocalEnvironment()
                {
                    Freq = new ConstantSignal(0, count),
                    Pitch = new ConstantSignal(note.n, count),
                    Locals = new Dictionary<string, Signal>(),
                    Gate = new ConstantSignal(0, count),
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
        /// 音符を直ちに再生します。
        /// </summary>
        public void NoteOn(int n)
        {
            NoteOff(n);

            notes.Add(new MyNoteEvent()
            {
                cell = rootTree.Generate(),
                n = n
            });
        }

        /// <summary>
        /// 音符を直ちに停止します。
        /// </summary>
        public void NoteOff(int n)
        {
            var list = notes.FindAll(x => x.n == n).ToArray();
            foreach (var item in list)
            {
                releasedNotes.Add(item);
                notes.Remove(item);  // 遅いかも？
            }
        }
    }
}
