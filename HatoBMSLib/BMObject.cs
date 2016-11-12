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
    /// ロングノートは始点と終点を個別に持つとは言ってない。
    /// </summary>

    public class BMObject : IComparable<BMObject>
    {
        public BMObject(int bmsch, int subCh, int wavid, Rational measure)
        {
            this.BMSChannel = bmsch;
            this.BMSSubChannel = subCh;
            this.Wavid = wavid;
            this.Measure = measure;
        }

        public int CompareTo(BMObject b)
        {
            return Measure.CompareTo(b.Measure);
        }

        public int BMSChannel;  // in Hex (ex. Lane26(2PSC) is 38 )
        public int BMSSubChannel;
        public int Wavid;  // in 36th (特に、標準BPM定義においては16進文字列を36進として解釈した値が格納される。)
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
            internal set;  // privateにしたい・・・
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

        internal bool IsNotChannel1X2X()
        {
            int hc = BMSChannel / 36;
            return (hc < 1 || 2 < hc);
        }

        /// <summary>
        /// 音声を持つオブジェクトであるかどうかを調べます。
        /// BMSの音声再生のために用います。
        /// ・BGM(#mmm01:)に対してはtrueを返します。
        /// ・LNObjでない通常オブジェと、ロングノートの始点に対しては、trueを返します。
        /// ・LNObj等のロングノートの終点に対しては、falseを返しますが、
        ///   EditModeがfalseの場合は、そのようなBMObjectはPlayableBMObjectの中にはありません。
        /// ・不可視オブジェに対してはtrueを返します。
        /// ・地雷に対してはfalseを返します。
        /// ・BGA/Layer/Poorに対してはfalseを返します。
        /// </summary>
        public bool IsSound()
        {
            // 注：LNObjに関しては、WavidがWAVファイルを指していないのでfalse

            int hc = BMSChannel / 36;
            int lc = BMSChannel % 36;
            return ((1 <= hc && hc <= 6) || BMSChannel == 1) && !IsLongNoteTerminal;
        }

        /// <summary>
        /// 画面描画するオブジェクトかどうかを調べます。
        /// この意味に関しては、何度か意味が変化してきました。
        /// ・BGM(#mmm01:)に対してはfalseを返します。
        /// ・LNObjでない通常オブジェと、ロングノートの始点に対しては、trueを返します。
        /// ・LNObj等のロングノートの終点に対しては、trueを返しますが、
        ///   EditModeがfalseの場合は、そのようなBMObjectはPlayableBMObjectの中にはありません。
        /// ・不可視オブジェに対してはfalseを返します。これを含める必要がある場合は、 IsPlayable() || IsInvisible() としてください。
        /// ・地雷に対してはtrueを返します。
        /// ・BGA/Layer/Poorに対してはfalseを返します。
        /// 
        /// また、IsSound() && IsPlayable() は、LNObjでない通常オブジェとロングノートの始点に対してtrueを返します。
        /// </summary>
        public bool IsPlayable()
        {
            // 画面描画するオブジェクトかどうかを調べます。

            // 注：LNObjに関しては
            //   ・trueを返す (エディタモードの場合？多分)
            //   ・そもそもそのようなBMObjectは存在しない（プレイモードの場合）
            // また、Invisibleではfalseを返します。

            // TODO:
            int hc = BMSChannel / 36;
            int lc = BMSChannel % 36;
            return (1 <= hc && hc <= 2) || (5 <= hc && hc <= 6) || (0xD <= hc && hc <= 0xE);
        }

        public bool IsLandmine()
        {
            int hc = BMSChannel / 36;
            return (0xD <= hc && hc <= 0xE);
        }

        public bool IsBackSound()
        {
            return BMSChannel == 1 && !IsLongNoteTerminal;
        }

        /// <summary>
        /// 不可視オブジェに対してのみtrueを返します。
        /// </summary>
        public bool IsInvisible()
        {
            int hc = BMSChannel / 36;
            return (3 <= hc && hc <= 4);
        }

        public bool IsGraphic()
        {
            return BMSChannel == 4 | BMSChannel == 6 | BMSChannel == 7;
        }

        /// <summary>
        /// 演奏デバイスの各ボタンに対応するkeyidを返します。
        /// あまりにもよく使うのでプロパティ化しました。
        /// パフォーマンスに影響が出たりします？？
        /// </summary>
        public int Keyid  // メソッドにするべきか、それともプロパティにするべきか。それが問題だ。
        {
            get
            {
                return (BMSChannel + 36) % 72;
            }
        }

        public override string ToString()
        {
            int integPart = (int)Math.Floor((double)Measure);

            Rational decimalPart = Measure - integPart;

            return "#" + integPart.ToString("D3") + " " + BMConvert.ToBase36(this.BMSChannel) + "\t" + decimalPart.ToString()
                + (IsGraphic() ? "\t#BMP" : "\t#WAV") + BMConvert.ToBase36(Wavid);
        }

        public override bool Equals(object obj)
        {
            BMObject bm = obj as BMObject;

            if (bm == null) return false;

            return (
                bm.BMSChannel == BMSChannel &&
                bm.Wavid == Wavid &&
                bm.Measure == Measure &&
                ((bm.Terminal == null) == (Terminal == null)) &&
                ((Terminal == null) ? true : bm.Terminal.Equals(Terminal)));
        }

        public override int GetHashCode()
        {
            return BMSChannel ^ Wavid ^ Measure.GetHashCode() ^ ((Terminal == null) ? 0 : Terminal.GetHashCode());
        }
    }
}
