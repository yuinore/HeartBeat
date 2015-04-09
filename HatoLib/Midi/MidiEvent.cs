using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HatoLib.Midi
{
    /// <summary>
    /// Midiイベント、SysExイベント、又は
    /// </summary>
    public abstract class MidiEvent : IComparable<MidiEvent>
    {
        public int tick;
        public int ch;
        
        public MidiEvent() { }
        public MidiEvent(MidiEvent me)
        {
            this.tick = me.tick;
            this.ch = me.ch;
        }
        public abstract MidiEvent Clone();

        public int CompareTo(MidiEvent obj)
        {
            if (obj is MidiEvent)
            {
                MidiEvent me2 = (MidiEvent)obj;

                if (this.tick != me2.tick) return this.tick - me2.tick;
                if (this is MidiEventMeta && ((MidiEventMeta)this).val == 0x2F) return 1;
                if (me2 is MidiEventMeta && ((MidiEventMeta)me2).val == 0x2F) return -1;

                if (this is MidiEventNote && me2 is MidiEventNote
                    && ((MidiEventNote)this).v != ((MidiEventNote)me2).v)
                    return ((MidiEventNote)this).v - ((MidiEventNote)me2).v;  // ノートオフが先

                return 0;
            }
            else
            {
                throw new ArgumentException("MidiEventではない何か");
            }
        }
        public abstract override string ToString();  // 改行で終了しなければならない
    }

    public class MidiEventNote : MidiEvent
    {
        // 8n, 9n
        public int n;
        public int v;  // v == 0 でノートオフを表すわけではない！！！と思ったけどノートオフは実装やめよう
        public int q;  // gate. 実質的な音符の長さ
        
        public MidiEventNote() { }
        public MidiEventNote(MidiEventNote me)
            : base(me)
        {
            this.n = me.n;
            this.v = me.v;
            this.q = me.q;
        }
        public override MidiEvent Clone() { return new MidiEventNote(this); }

        public override string ToString()
        {
            return "t=" + tick + "\tch=" + (ch + 1) +
                "\tNote\tn=" + n + "\tv=" + v + "\tq=" + q + "\n";
        }

        /// <summary>
        /// mml形式の文字列から単一のmidieventを作成します。
        /// </summary>
        public static MidiEventNote NewFromQuery(string query, int timebase)
        {
            // v00L00o00x, v00L00_00o00x の形式にとりあえず対応したい
            LazyMatch lazy = new LazyMatch(query, @"\Av([0-9]+)L([0-9]+)(?:(?:_|\-)([0-9]+))?o([0-9])([a-g])(\+|\#|p|\-)?\Z", RegexOptions.IgnoreCase);
            Match match;
            if (lazy.Evaluate(out match))
            {
                // ベロシティ
                int vel = Convert.ToInt32(match.Groups[1].Captures[0].Value);
                if (vel > 127) throw new Exception("ベロシティが無効です。");

                // 分母 (例:16なら16分音符)
                int len1 = Convert.ToInt32(match.Groups[2].Captures[0].Value);

                // 分子
                int len2 = 1; 
                if (match.Groups[3].Captures.Count >= 1)
                {
                    len2 = Convert.ToInt32(match.Groups[3].Captures[0].Value);
                }

                // オクターブ
                int oct = Convert.ToInt32(match.Groups[4].Captures[0].Value);

                // c,d,e,f,g,a,b のどれか。それぞれ、ド、レ、ミ、フ、ァ、ソ、ラ、シ。
                string alphabet = match.Groups[5].Captures[0].Value;

                // #, +, -, または指定なし（null）
                string sharpflat = null;
                if (match.Groups[6].Captures.Count >= 1)
                {
                    sharpflat = match.Groups[6].Captures[0].Value;
                }

                int noteNum = "c-d-ef-g-a-b".IndexOf(alphabet.ToLower()[0]) + oct * 12;  // オクターブ 0 の c が noteNum=0
                noteNum += sharpflat == null ? 0 : sharpflat == "-" ? -1 : +1;

                if (noteNum < 0 || 127 < noteNum) throw new Exception("ノート番号が無効です。");

                return new MidiEventNote()
                {
                    ch = 0,
                    n = noteNum,
                    tick = 0,
                    v = vel,
                    q = (int)(timebase * (long)len2 / len1)
                };
            }
            else
            {
                throw new Exception("mmlが無効です");
            }
        }
    }

    public class MidiEventCC : MidiEvent
    {
        // Bn
        public int cc;
        public int val;
        
        public MidiEventCC() { }
        public MidiEventCC(MidiEventCC me)
            : base(me)
        {
            this.cc = me.cc;
            this.val = me.val;
        }
        public override MidiEvent Clone() { return new MidiEventCC(this); }

        public override string ToString()
        {
            return "t=" + tick + "\tch=" + (ch + 1) +
                "\tCC " + cc + " = " + val + "\n";
        }
    }

    public class MidiEventKeyPressure : MidiEvent
    {
        // An
        public int n;
        public int val;
        
        public MidiEventKeyPressure() { }
        public MidiEventKeyPressure(MidiEventKeyPressure me)
            : base(me)
        {
            this.n = me.n;
            this.val = me.val;
        }
        public override MidiEvent Clone() { return new MidiEventKeyPressure(this); }

        public override string ToString()
        {
            return "t=" + tick + "\tch=" + (ch + 1) +
                "\tPolyphonicKeyPressure\tn=" + n + "\tval=" + val + "\n";
        }
    }

    public class MidiEventChannelPressure : MidiEvent
    {
        // Dn
        public int val;
       
        public MidiEventChannelPressure() { }
        public MidiEventChannelPressure(MidiEventChannelPressure me)
            : base(me)
        {
            this.val = me.val;
        }
        public override MidiEvent Clone() { return new MidiEventChannelPressure(this); }

        public override string ToString()
        {
            return "t=" + tick + "\tch=" + (ch + 1) +
                "\tChannelPressure\tval=" + val + "\n";
        }
    }

    public class MidiEventProgram : MidiEvent
    {
        // Cn
        public int val;
        
        public MidiEventProgram() { }
        public MidiEventProgram(MidiEventProgram me)
            : base(me)
        {
            this.val = me.val;
        }
        public override MidiEvent Clone() { return new MidiEventProgram(this); }

        public override string ToString()
        {
            return "t=" + tick + "\tch=" + (ch + 1) +
                "\tProgramChange\tval=" + val + "\n";
        }
    }

    public class MidiEventPB : MidiEvent
    {
        // En
        public int val;
        
        public MidiEventPB() { }
        public MidiEventPB(MidiEventPB me)
            : base(me)
        {
            this.val = me.val;
        }
        public override MidiEvent Clone() { return new MidiEventPB(this); }

        public override string ToString()
        {
            return "t=" + tick + "\tch=" + (ch + 1) +
                "\tPitchBend\tval=" + val + "\n";
        }
    }

    public class MidiEventSysEx : MidiEvent
    {
        // 0xF0, 0xF7
        public byte[] bytes;  // 注意：頭のF0を含まない
        public int stbyte;

        public MidiEventSysEx() { }
        public MidiEventSysEx(MidiEventSysEx me)
            : base(me)
        {
            this.bytes = (byte[])me.bytes.Clone();
            int stbyte = me.stbyte;
        }
        public override MidiEvent Clone() { return new MidiEventSysEx(this); }

        public override string ToString()
        {
            return "t=" + tick + "\t" + //"\tch=" + ch +
                "\tSysEx\tbytes = " + BitConverter.ToString(bytes) + "\n";
        }
    }

    public class MidiEventMeta : MidiEvent
    {
        // statusbyte == 0xFF
        public int id;

        //public String name;  // ex: "TrackName"
        public byte[] bytes;  // ex: "TrackName" (in byte[])
        public String text;  // ex: "Piano 1"
        public int val;  // tempo, port, etc...

        public MidiEventMeta() { }
        public MidiEventMeta(MidiEventMeta me)
            : base(me)
        {
            //this.name = me.name;
            this.bytes = (byte[])me.bytes.Clone();
            this.text = me.text;
            this.val = me.val;
            this.id = me.id;
        }
        public override MidiEvent Clone() { return new MidiEventMeta(this); }

        public override string ToString()
        {
            String text2 = text;
            switch (id)
            {
                case 0x20:
                case 0x21:
                case 0x51:
                    text = val.ToString();
                    break;
            }
            return "t=" + tick + "\t" + //"\tch=" + ch +
                "\tMeta " + name + " = " + text + "\n";
        }

        public String name
        {
            get
            {
                switch (this.id)
                {
                    case 0x03:  // Track Name
                        return "Track Name";
                    case 0x04:
                        return "Instrument Name";
                    case 0x05:
                        return "Lyric";
                    case 0x06:
                        return "Marker";

                    case 0x21:
                        return "Port";
                    case 0x2F:  // End of Track
                        return "End of Track";
                    case 0x51:
                        return "Tempo (usec per beat)";
                    case 0x54:
                        return "SMPTE Offset";
                    case 0x58:
                        return "Signature";
                    case 0x59:
                        return "Key";
                    default:
                        return "Other (" + this.id + ")";
                }
            }
        }
    }
}
