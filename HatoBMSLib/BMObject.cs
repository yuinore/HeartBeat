using HatoLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* 
-----------------------------------
BMS Channels

01	[objs/wav] BGM
02	[number] Measure Length
03	[objs/number] BPM
04	[objs/bmp] BGA-BASE
05
06	[objs/bmp] BGA-POOR
07	[objs/bmp] BGA-LAYER
08	[objs/tempo] Extended BPM
09	[objs/stop] Stop Sequence

1x	[objs/wav] 1 Player
2x	[objs/wav] 2 Player
3x	[objs/wav] 1 Player Invisible
4x	[objs/wav] 2 Player Invisible
5x	[objs/wav] 1 Player Long Note
6x	[objs/wav] 2 Player Long Note

99	[objs/text]
A0	[objs/exrank]
Dx	[objs/wav] 1 Player Mine
Ex	[objs/wav] 2 Player Mine
-----------------------------------
BMS Definitions

#WAVxx	[xx = obj/wav] [filename]
#BMPxx	[xx = obj/bmp] [filename]  // 動画も可

#BPMxx	[xx = obj/bpm] [number]
// #BGAxx	[xx = obj/bmp??] [filename]
#STOPxx	[xx = onj/stop] [number]  // 1は192分音符相当

#LNOBJ	[obj/wav]

*/

namespace HatoBMSLib
{
    /// <summary>
    /// Be-Music Object.
    /// 
    /// BMSにおけるオブジェを表します。これは、BMSに配置された、２桁の３６進数です。
    /// ロングノートは始点と終点を個別に持ちます。
    /// ん！？これはreadonlyじゃないぞ！！！！殺せーーー！！！！
    /// コピーコンストラクタの呼び出し（代入のこと）を禁止できればいいんだけれど・・・
    /// なぜ値型にしたのかというと、ヒープにメモリを確保するオーバーヘッドを回避したかったから。
    /// でもstructってスタックに確保してくれるんでしょうか？不変型にするとnewの回数が増えると思うのですが
    /// ＞ただし、構造体をインスタンス化した場合は、スタックに作成されます。これによりパフォーマンスが向上します。
    /// ＞http://msdn.microsoft.com/ja-jp/library/aa288471(v=vs.71).aspx
    /// あーそりゃそうか
    /// </summary>
    
    public class BMObject : IComparable<BMObject>
    {
        public BMObject(int bmsch, int wavid, Rational measure)
        {
            this.BMSChannel = bmsch;
            this.Wavid = wavid;
            this.Measure = measure;
        }

        public int CompareTo(BMObject b)
        {
            return Measure.CompareTo(b.Measure);
        }

        public int BMSChannel;  // in Hex (ex. Lane26(2PSC) is 38 )
        public int Wavid;  // in 36th
        public Rational Measure;  // 理論上の再生地点で、ゲーム再生には用いない

        public double Beat;
        public double Seconds;  // 秒
        public double Disp;  // 通常はbeatに比例

        public bool Broken;  // 破壊されたか？
        public Judgement Judge;
        public double BrokeAt;  // 破壊された時刻（秒）

        public BMObject Terminal;

        // IsPlayable()がfalseのとき、この値はfalseとなることが望まれる。
        public bool IsLongNoteTerminal
        {
            get;
            internal set;
        }

        /*public BMObjectType Type
        {
            get
            {
                return BMSStruct.BMSChannelToObjectType(ch);
            }
        }*/

        internal bool IsChannel5X6X()
        {
            int hc = BMSChannel / 36;
            return (5 <= hc && hc <= 6);
        }
        public bool IsSound()
        {
            // 注：LNObjに関しては、WavidがWAVファイルを指していないのでfalse

            int hc = BMSChannel / 36;
            int lc = BMSChannel % 36;
            return ((1 <= hc && hc <= 6) || BMSChannel == 1) && !IsLongNoteTerminal;
        }

        public bool IsPlayable()
        {
            // 注：LNObjに関しては
            //   ・trueを返す (エディタモードの場合？多分)
            //   ・そもそもそのようなBMObjectは存在しない（プレイモードの場合）
            // また、Invisibleではfalseを返します。

            // TODO:
            int hc = BMSChannel / 36;
            int lc = BMSChannel % 36;
            return (1 <= hc && hc <= 2) || (5 <= hc && hc <= 6) || (0xD <= hc && hc <= 0xE);
        }

        public bool IsInvisible()
        {
            int hc = BMSChannel / 36;
            return (3 <= hc && hc <= 4);
        }

        public bool IsGraphic()
        {
            return BMSChannel == 4 | BMSChannel == 6 | BMSChannel == 7;
        }

        /*public override string ToString()
        {
            long n2 = Beat.n;
            long d2 = Beat.d;
            while (d2 < 16)
            {
                n2 *= 2;
                d2 *= 2;
            }
            return "#" + (n2 / d2).ToString("D3") + "\t" + (n2 % d2) + "/" + d2 + "\t#WAV" + BMSParser.IntToHex36Upper(MixerChannel);
        }*/
    }
}
