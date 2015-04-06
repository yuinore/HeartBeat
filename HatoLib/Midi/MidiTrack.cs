using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace HatoLib.Midi
{
    // MidiTrack が List<MidiEvent> を継承しているのは間違っているのでは・・・？
    // ということを Effective C++ を読んで思った

    // そもそも MidiTrackというクラスが必要なかったという可能性・・・？？

    // まあ今からこのクラスを消すのは面倒だからしないけれど

    // このクラスはフィールドを持ってはならない。
    // 言い換えると、 new MidiTrack(midiTrack.Select(x => x)) は、元の midiTrack と一致しなければならない。

    // ということは自作クラスを定義する必要は無いのでは！？！？

    public class MidiTrack : List<MidiEvent>, IEnumerable<MidiEvent>  // ソート関数OrderByを実装する
    {
        /// <summary>
        /// 空の MidiTrackを作成する。
        /// </summary>
        public MidiTrack()
        {
        }

        /// <summary>
        /// midiとして有効な(比較的)最小のトラックを作成する。
        /// </summary>
        /// <param name="trackname"></param>
        /// <param name="MIDI_TEXT_ENCODING"></param>
        public MidiTrack(String trackname)//, String MIDI_TEXT_ENCODING)
        {
            int port = 0;

            {  // Track Name
                MidiEventMeta me = new MidiEventMeta();
                me.tick = 0;
                me.ch = 0;
                me.id = 0x03;
                me.text = trackname;
                me.bytes = HatoEnc.Encode(trackname);
                me.val = 0;

                this.Add(me);

                // Instrument Name
                me = (MidiEventMeta)me.Clone();
                me.id = 0x04;
                this.Add(me);
            }
            {  // Port
                MidiEventMeta me = new MidiEventMeta();
                me.tick = 0;
                me.ch = 0;
                me.id = 0x21;
                me.text = "";
                me.bytes = new byte[1];
                me.bytes[0] = (byte)port;
                me.val = port;

                this.Add(me);
            }

        }

        /// <summary>
        /// コピーコンストラクタ
        /// </summary>
        /// <param name="iOrderedEnumerable"></param>
        public MidiTrack(IEnumerable<MidiEvent> iOrderedEnumerable)
            : base(iOrderedEnumerable)
        {
            // TODO: Complete member initialization
        }
        
        public void AddTempo(double BPM, MidiStruct midistruct)
        {
            MidiEventMeta tempometa = new MidiEventMeta();
            tempometa.ch = 0;
            tempometa.tick = 0;
            tempometa.id = 0x51;  // tempo
            tempometa.val = (int)(60.0 * 1000000 / BPM);
            if (tempometa.val > 0x1000000) tempometa.val = 0xFFFFFF;
            tempometa.bytes = new byte[3];
            tempometa.bytes[0] = (byte)((tempometa.val >> 16) & 0xFF);
            tempometa.bytes[1] = (byte)((tempometa.val >> 8) & 0xFF);
            tempometa.bytes[2] = (byte)((tempometa.val >> 0) & 0xFF);
            this.Add(tempometa);
        }

        public void AddEndOfTrack(MidiStruct midistruct)
        {
            MidiEventMeta endOfTrack = new MidiEventMeta();
            endOfTrack.ch = 0;
            endOfTrack.tick = ((this.Count >= 1) ? this[this.Count - 1].tick : 0) + midistruct.BeatsToTicks(4);
            endOfTrack.id = 0x2F;  // end of track
            endOfTrack.bytes = new byte[0];
            endOfTrack.val = 0;
            this.Add(endOfTrack);
        }

        public override String ToString()
        {
            StringBuilder s0 = new StringBuilder();
            for (int i = 0; i < this.Count; i++)
            {
                s0.Append(this[i].ToString());
            }
            return s0.ToString();
        }

    }

}
