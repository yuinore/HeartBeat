﻿using HatoLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HatoBMSLib
{
    /// <summary>
    /// Be-Music Script Struct.
    /// </summary>
    public class BMSStruct
    {
        public string DirectoryName;
        public string BMSFilePath;

        /// <summary>
        /// BMSのExportを伴うかどうかを設定します。trueの場合、暗黙の副題等の処理を行いません。
        /// </summary>
        public readonly bool EditorMode = false;  // BMSの読み込み前に設定されなければならないので、コンストラクタから設定しなければなりません。

        //******************************************//
        // BMS Fields (未指定の場合は空文字ではなくnullが入る)
        public string Title = "";
        public string Subtitle;
        public string Artist = "";
        public string Subartist;
        public string Genre = "";
        public string Comment;

        public string Stagefile;
        public string BackBMP;
        public string Banner;

        // TODO: 初期値を定義しなかった場合に警告を出す
        public int Player = 1;
        public int Playlevel = 0;
        public int Rank = 3;
        public int Difficulty = 0;
        public double? Total;
        public int LNType = 1;
        public int? LNObj;

        public double BPM = 0;
        //******************************************//
        // BMS Objects and Definition

        // #mmm02:の行が無い場合でも、デフォルトテンポの指定が入るため、空にはなりません。
        public Transport transp;

        public List<KeyValuePair<string, string>> BMSHeader = new List<KeyValuePair<string, string>>();
        public List<string> ExtensionFields = new List<string>();

        private List<BMObject> AllBMObjects = new List<BMObject>();

        // SoundBMObjects と GraphicBMObjects と OtherBMObjects は、
        // 任意の2つの積集合が空集合で、和集合がAllBMObjectsから####LN終端を除いたもの####に等しいと見せかけて、transport関連も含まれていないですねこれは・・・。
        public List<BMObject> SoundBMObjects = new List<BMObject>();  // IsSound => true
        public List<BMObject> GraphicBMObjects = new List<BMObject>();  // IsGraphic => true
        public List<BMObject> OtherBMObjects = new List<BMObject>();

        public List<BMObject> PlayableBMObjects = new List<BMObject>();

        public Dictionary<int, string> WavDefinitionList = new Dictionary<int, string>();
        public Dictionary<int, string> SynthDefinitionList = new Dictionary<int, string>();
        public Dictionary<int, string> BitmapDefinitionList = new Dictionary<int, string>();
        public Dictionary<int, double> BPMDefinitionList = new Dictionary<int, double>();
        public Dictionary<int, double> StopDefinitionList = new Dictionary<int, double>();

        //******************************************//
        // その他内部用フィールド

        BMSExceptionHandler ExceptionHandler = new BMSExceptionHandler();

        //******************************************//

        public string Message
        {
            get
            {
                return ExceptionHandler.Meesage;
            }
        }

        public string ToFullPath(string filename)
        {
            if (filename.ToLower().StartsWith("synth:")) return filename;

            if (DirectoryName == null) throw new Exception("Directoryメンバが設定されていません。");
            return Path.Combine(DirectoryName, filename);
        }

        delegate bool TryParse<T>(string text, out T val) where T : IComparable;

        /// <summary>
        /// フィールドに値を設定します。正しく設定できた場合はtrueを返します。
        /// minInclusive &lt;= defaultValue &lt;= maxInclusive を満たす必要はありません。
        /// </summary>
        /// <param name="name">フィールド名。先頭にシャープを含むべきです。</param>
        /// <param name="text">変換対象の文字列</param>
        /// <param name="val">変換先のフィールド</param>
        /// <param name="defaultValue">変換できなかった場合に指定される値</param>
        /// <param name="minInclusive">許容される最小値</param>
        /// <param name="maxInclusive">許容される最大値</param>
        /// <returns></returns>
        private bool SetField<T>(string name, string text, out T val, T defaultValue, T minInclusive, T maxInclusive, TryParse<T> tryparse) where T : IComparable
        {
            if (tryparse(text, out val))
            {
                if (val.CompareTo(minInclusive) < 0 || val.CompareTo(maxInclusive) > 0)
                {
                    ExceptionHandler.ThrowFormatWarning(name + "の値が範囲外です。");
                    val = defaultValue;
                    return false;
                }
            }
            else
            {
                ExceptionHandler.ThrowFormatWarning(name + "を数値に変換できません。");
                val = defaultValue;
                return false;
            }
            return true;
        }

        public void Export(string filename)
        {
            // warning: the file will be overwritten
            Export(new FileStream(filename, FileMode.Create, FileAccess.Write));
        }

        public void Export(Stream ws)
        {
            // ソフラン等、一部非対応の機能があります。
            // まだコーディング途中の部分がありますのでご注意ください。

            // 未対応:
            //  transp関連
            //  #COMMENT

            //  気をつけて：画像expansionじゃない 空定義消える


            //  rank 5 が読み込まれない問題とか

            List<BMObject> transpObjs;
            SortedDictionary<int, double> transpBPMDef;
            SortedDictionary<int, double> transpStopDef;

            double initialBPM;

            transp.Export(out transpObjs, out transpBPMDef, out transpStopDef, out initialBPM);

            if (Math.Abs(initialBPM - BPM) >= 0.001)
            {
                throw new Exception("穏やかじゃないわね");
            }

            var writer = new StreamWriter(ws, Encoding.GetEncoding("Shift_JIS"));

            writer.WriteLine();
            writer.WriteLine("*---------------------- HEADER FIELD");
            writer.WriteLine();

            writer.WriteLine("#PLAYER " + Player);
            writer.WriteLine("#GENRE " + Genre);
            writer.WriteLine("#TITLE " + Title);
            writer.WriteLine("#ARTIST " + Artist);
            writer.WriteLine("#BPM " + BPM);
            writer.WriteLine("#PLAYLEVEL " + Playlevel);
            writer.WriteLine("#RANK " + Rank);

            writer.WriteLine();

            if (Subtitle != null) writer.WriteLine("#SUBTITLE " + Subtitle);
            if (Subartist != null) writer.WriteLine("#SUBARTIST " + Subartist);
            if (Stagefile != null) writer.WriteLine("#STAGEFILE " + Stagefile);
            if (Banner != null) writer.WriteLine("#BANNER " + Banner);
            if (BackBMP != null) writer.WriteLine("#BACKBMP " + BackBMP);

            writer.WriteLine();

            if (Difficulty != 0) writer.WriteLine("#DIFFICULTY " + Difficulty);
            //if (ExRank != 0) writer.WriteLine("#EXRANK " + ExRank);
            if (Total != null) writer.WriteLine("#TOTAL " + Total);
            if (Comment != null) writer.WriteLine("#COMMENT " + Comment);
            if (LNObj != null && LNObj != 0)
            {
                writer.WriteLine("#LNOBJ " + BMConvert.ToBase36((int)LNObj));
            }else
            {
                writer.WriteLine("#LNTYPE " + LNType);
            }

            writer.WriteLine();

            foreach (var kvpair in WavDefinitionList.OrderBy(x => x.Key))
            {
                if (kvpair.Value == null || kvpair.Value == "") continue;

                writer.WriteLine("#WAV" + BMConvert.ToBase36(kvpair.Key) + " " + kvpair.Value);
            }

            foreach (var kvpair in transpBPMDef.OrderBy(x => x.Key))
            {
                writer.WriteLine("#BPM" + BMConvert.ToBase36(kvpair.Key) + " " + kvpair.Value);
            }

            foreach (var kvpair in transpStopDef.OrderBy(x => x.Key))
            {
                writer.WriteLine("#STOP" + BMConvert.ToBase36(kvpair.Key) + " " + kvpair.Value);
            }

            writer.WriteLine();
            writer.WriteLine();

            if (BitmapDefinitionList.Any(kvpair => (kvpair.Value != null && kvpair.Value != "")) || BMSHeader.Count != 0 || ExtensionFields.Count != 0)
            {
                writer.WriteLine("*---------------------- EXPANSION FIELD");

                foreach (var kvpair in BitmapDefinitionList.OrderBy(x => x.Key))
                {
                    if (kvpair.Value == null || kvpair.Value == "") continue;

                    writer.WriteLine("#BMP" + BMConvert.ToBase36(kvpair.Key) + " " + kvpair.Value);
                }

                foreach (var expansion in BMSHeader)
                {
                    writer.WriteLine("#" + expansion.Key + (expansion.Value == "" ? "" : (" " + expansion.Value)));
                    // ↑ここがWriteになってたのが原因か！！
                }

                foreach (var expansion in ExtensionFields)
                {
                    writer.WriteLine(expansion);
                    // ↑ここがWriteになってたのが原因か！！
                }

                writer.WriteLine();
                writer.WriteLine();
                writer.WriteLine();
            }

            writer.WriteLine("*---------------------- MAIN DATA FIELD");
            writer.WriteLine();
            
            var allobjs_wo_terminals = SoundBMObjects.Concat(GraphicBMObjects).Concat(OtherBMObjects).Concat(transpObjs).ToArray();

            var terminalObjs = allobjs_wo_terminals.Where(x => x.Terminal != null).Select(x => x.Terminal);

            var allobjs = allobjs_wo_terminals.Concat(terminalObjs);

            // 注意点: terminalObjsがちゃんとterminalになってるかどうかとかのチェックは頑張って

            // Tupleはすごい
            // #XXX02: が #XXX01: よりも前に来るとか・・・まじですか・・・
            var groups = allobjs
                .GroupBy(x => Tuple.Create((int)Math.Floor((double)x.Measure), x.BMSChannel, x.BMSSubChannel))
                .OrderBy(x => x.Key.Item2 == 2 ? Tuple.Create(x.Key.Item1, -2, x.Key.Item3) : x.Key)
                .ToArray();

            Tuple<int, int, int> lastKey = new Tuple<int, int, int>(-1, -1, -1);

            foreach(var g in groups)
            {
                if(lastKey.Item1 != g.Key.Item1)
                {
                    Enumerable.Range(0, g.Key.Item1 - lastKey.Item1).ToList().ForEach(x =>
                    {
                        writer.WriteLine();  // 次の小節へ
                    });
                }

                if (g.Key.Item2 == 2)
                {
                    // Signature
                    var ev = (BMObjectSignature)(g.First());

                    writer.WriteLine("#" + String.Format("{0:000}", g.Key.Item1) + BMConvert.ToBase36(g.Key.Item2) + ":" + ev.Signature);
                }
                else
                {
                    // Signature以外

                    if (g.Key.Item2 == 1)
                    {
                        // BGMレーン(SubChannelを持つ)
                        int lastSubChannel = ((lastKey.Item1 == g.Key.Item1) && (lastKey.Item2 == g.Key.Item2)) ? lastKey.Item3 : -1;
                        int targetSubChannel = g.Key.Item3;

                        // 本当にこれでいいのか？？？
                        Enumerable.Range(0, targetSubChannel - lastSubChannel - 1).ToList().ForEach(x =>
                        {
                            writer.WriteLine("#" + String.Format("{0:000}", g.Key.Item1) + "01:00");
                        });
                    }

                    long lcm = 1;

                    foreach (var e in g)
                    {
                        lcm = Lcm(lcm, e.Measure.Denominator);
                    }

                    if (lcm > 8192) throw new Exception("それはやばい");

                    var objs = g.Select(x => x).OrderBy(x => (int)((x.Measure - g.Key.Item1) * lcm)).ToArray();

                    writer.Write("#" + String.Format("{0:000}", g.Key.Item1) + BMConvert.ToBase36(g.Key.Item2) + ":");

                    int cursor = 0;
                    foreach (var obj in objs)
                    {
                        int tick = (int)((obj.Measure - g.Key.Item1) * lcm);

                        if (cursor > tick) throw new Exception("不正なBMSデータです。オブジェの重複を確認してください。");

                        while (cursor != tick)
                        {
                            writer.Write("00");
                            cursor++;
                        }

                        if (obj.Wavid == 0) throw new Exception("不正なBMSデータです。定義番号を確認してください。");

                        writer.Write(BMConvert.ToBase36(obj.Wavid));
                        cursor++;
                    }

                    while (cursor != lcm)
                    {
                        writer.Write("00");
                        cursor++;
                    }

                    writer.WriteLine();
                }

                lastKey = g.Key;
            }

            writer.WriteLine();
            writer.WriteLine();

            writer.Close();
        }

        // Thanks to http://qiita.com/gushwell/items/f08d0e71fa0480dbb396
        private static long Lcm(long a, long b)
        {
            return a * b / Gcd(a, b);
        }

        private static long Gcd(long a, long b)
        {
            if (a < b)
            {
                return Gcd(b, a);
            }
            while (b != 0)
            {
                var remainder = a % b;
                a = b;
                b = remainder;
            }
            return a;
        }

        private bool SetNullableField<T>(string name, string text, out Nullable<T> val, Nullable<T> defaultValue, T minInclusive, T maxInclusive, TryParse<T> tryparse) where T : struct, IComparable
        {
            T outval;

            if (tryparse(text, out outval))
            {
                if (outval.CompareTo(minInclusive) < 0 || outval.CompareTo(maxInclusive) > 0)
                {
                    ExceptionHandler.ThrowFormatWarning(name + "の値が範囲外です。");
                    val = defaultValue;
                    return false;
                }
                val = outval;
            }
            else
            {
                ExceptionHandler.ThrowFormatWarning(name + "を数値に変換できません。");
                val = defaultValue;
                return false;
            }
            return true;
        }

        private bool SetField(string name, string text, out int val, int defaultValue, int minInclusive, int maxInclusive)
        {
            return SetField(name, text, out val, defaultValue, minInclusive, maxInclusive, Int32.TryParse);
        }

        private bool SetField(string name, string text, out double val, double defaultValue, double minInclusive, double maxInclusive)
        {
            return SetField(name, text, out val, defaultValue, minInclusive, maxInclusive, Double.TryParse);
        }

        public BMSStruct(string path) :
            this(path, false)
        {
        }

        // このメソッドでは例外を出すべきではない。
        public BMSStruct(string path, bool editormode)
        {
            EditorMode = editormode;

            DirectoryName = Path.GetDirectoryName(path);
            BMSFilePath = path;
            transp = new Transport(ExceptionHandler);

            Dictionary<int, int> readBgmLinesCount = new Dictionary<int, int>();

            #region BMSファイルのパース
            using (var r = new RandomBMSReader(
                new FileStream(path, FileMode.Open, FileAccess.Read),
                Encoding.GetEncoding("Shift_JIS"), EditorMode, ExceptionHandler))
            {
                var linenumber = 0;

                while (!r.EndOfStream)
                {
                    var line = r.ReadLine();
                    linenumber++;
                    
                    if (Regex.IsMatch(line, @"^\s*#.+$", RegexOptions.Compiled))
                    {
                        // コメントでない行
                        Match match;

                        // 小節長指定行にマッチ
                        var MatchMeasureLengthLine = new LazyMatch(line,
                            @"^\s*#(\d\d\d)02:([0-9\.]+)$", RegexOptions.Compiled);

                        // オブジェ行
                        var MatchDataLine = new LazyMatch(line,
                            @"^\s*#(\d\d\d)([0-9A-Za-z][0-9A-Za-z]):((?:[0-9A-Za-z][0-9A-Za-z])+)$", RegexOptions.Compiled);

                        // ヘッダ行
                        var MatchHeaderLine = new LazyMatch(line,
                            @"^\s*#([^\d\s]\S*)\s*(.*)$", RegexOptions.Compiled);

                        if (MatchMeasureLengthLine.Evaluate(out match))
                        {
                            #region 小節長指定行にマッチ
                            int measure = Convert.ToInt32(match.Groups[1].Captures[0].Value);
                            var contents = Convert.ToDouble(match.Groups[2].Captures[0].Value);

                            transp.AddSignature(measure, contents);
                            #endregion
                        }
                        else if (MatchDataLine.Evaluate(out match))
                        {
                            #region データ行にマッチ
                            int measure = Convert.ToInt32(match.Groups[1].Captures[0].Value);
                            int bmsch = BMConvert.FromBase36(match.Groups[2].Captures[0].Value);
                            int subCh = 0;
                            var contents = match.Groups[3].Captures[0].Value;

                            if(bmsch == 1)
                            {
                                // BGM Channel
                                if (!readBgmLinesCount.TryGetValue(measure, out subCh))
                                {
                                    subCh = 0;
                                }
                                readBgmLinesCount[measure] = subCh + 1;
                            }

                            Rational measureTime;
                            int partitions = contents.Length / 2;
                            for (int i = 0; i < partitions; i++)
                            {
                                int wavid = BMConvert.FromBase36(contents.Substring(i * 2, 2));
                                if (wavid != 0)
                                {
                                    measureTime = measure + new Rational(i, partitions);

                                    AllBMObjects.Add(new BMObject(bmsch, subCh, wavid, measureTime));
                                }
                            }
                            #endregion
                        }

                        else if (MatchHeaderLine.Evaluate(out match))
                        {
                            #region ヘッダ行にマッチ（内容があるかどうかは問わない）
                            var header = match.Groups[1].Captures[0].Value;
                            var val = match.Groups[2].Captures[0].Value;

                            var MatchWavXX = new LazyMatch(header, @"^WAV([0-9A-Za-z][0-9A-Za-z])$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            var MatchSynthXX = new LazyMatch(header, @"^SYNTH([0-9A-Za-z][0-9A-Za-z])$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            var MatchBMPXX = new LazyMatch(header, @"^BMP([0-9A-Za-z][0-9A-Za-z])$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            var MatchBPMXX = new LazyMatch(header, @"^BPM([0-9A-Za-z][0-9A-Za-z])$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            var MatchSTOPXX = new LazyMatch(header, @"^STOP([0-9A-Za-z][0-9A-Za-z])$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                            if (MatchWavXX.Evaluate(out match))
                            {
                                // #WAVxx にマッチ
                                int wavid = BMConvert.FromBase36(match.Groups[1].Captures[0].Value);  // ここでは例外チェックの必要は無いはず
                                WavDefinitionList.Add(wavid, val);
                            }
                            else if (MatchBMPXX.Evaluate(out match))
                            {
                                int wavid = BMConvert.FromBase36(match.Groups[1].Captures[0].Value);

                                if (BitmapDefinitionList.ContainsKey(wavid))
                                {
                                    ExceptionHandler.ThrowFormatWarning("#BMP" + BMConvert.ToBase36(wavid) + "が再定義されています。");
                                }
                                BitmapDefinitionList[wavid] = val;
                            }
                            else if (MatchBPMXX.Evaluate(out match))
                            {
                                int wavid = BMConvert.FromBase36(match.Groups[1].Captures[0].Value);
                                double vald;
                                SetField("#" + header, val, out vald, 120, Double.Epsilon, Double.MaxValue);
                                BPMDefinitionList.Add(wavid, vald);
                            }
                            else if (MatchSTOPXX.Evaluate(out match))
                            {
                                int wavid = BMConvert.FromBase36(match.Groups[1].Captures[0].Value);
                                double vald;
                                SetField("#" + header, val, out vald, 192, 0, Double.MaxValue);
                                StopDefinitionList.Add(wavid, vald);
                            }
                            else if (MatchSynthXX.Evaluate(out match))
                            {
                                int wavid = BMConvert.FromBase36(match.Groups[1].Captures[0].Value);
                                SynthDefinitionList.Add(wavid, val);
                            }
                            else
                            {
                                switch (header.ToUpper())
                                {
                                    case "TITLE": Title = val; break;
                                    case "SUBTITLE": Subtitle = val; break;
                                    case "ARTIST": Artist = val; break;
                                    case "SUBARTIST": Subartist = val; break;
                                    case "GENRE": Genre = val; break;
                                    case "COMMENT": Comment = val; break;
                                    case "STAGEFILE": Stagefile = val; break;
                                    case "BACKBMP": BackBMP = val; break;
                                    case "BANNER": Banner = val; break;
                                    case "BPM":
                                        SetField("#" + header, val, out BPM, 0, Double.Epsilon, Double.MaxValue, Double.TryParse);
                                        break;
                                    case "PLAYER":
                                        SetField("#" + header, val, out Player, 1, 1, 4);
                                        break;
                                    case "PLAYLEVEL":
                                        SetField("#" + header, val, out Playlevel, 0, 0, Int32.MaxValue);
                                        break;
                                    case "RANK":
                                        SetField("#" + header, val, out Rank, 0, 0, 4);
                                        break;
                                    case "DIFFICULTY":
                                        SetField("#" + header, val, out Difficulty, 1, 1, 5);
                                        break;
                                    case "TOTAL":
                                        SetNullableField("#" + header, val, out Total, 0, 0, Double.MaxValue, Double.TryParse);
                                        break;
                                    case "LNTYPE":
                                        SetField("#" + header, val, out LNType, 1, 1, 1);  // LNTYPE 2 には非対応
                                        break;
                                    case "LNOBJ":
                                        SetNullableField("#" + header, val, out LNObj, 0, 1, 36 * 36 - 1, BMConvert.TryParseFromBase36);
                                        break;
                                    default:
                                        ExceptionHandler.ThrowFormatWarning("未知のヘッダ文" + header + "を検出しました。");
                                        BMSHeader.Add(new KeyValuePair<string, string>(header.ToUpper(), val));
                                        break;
                                }
                            }

                            #endregion
                        }
                        else
                        {
                            // ヘッダ行でもデータ行でもない（これはキャッチされることを想定している、多分）
                            // たとえば、 #01:12345 （内容が奇数文字数）とか、 #7KEYS （頭が[0-9]のヘッダ文）とか。
                            ExceptionHandler.ThrowFormatWarning("BMSから不適切な行が検出されました。BMSの読み込みを中断します。 行：" + linenumber + ", \"" + line + "\"");

                            ExtensionFields.Add(line);
                        }

                    }
                }

                r.Close();
            }
            #endregion

            AllBMObjects.Sort();

            #region LN終端の探索・設定
            Dictionary<int, BMObject> prevobj = new Dictionary<int, BMObject>();
            
            foreach (var obj in AllBMObjects)
            {
                // 暫定設定
                obj.IsLongNoteTerminal = false;

                if (obj.IsPlayable() || obj.IsInvisible())
                {
                    // 暫定設定
                    obj.IsLongNoteTerminal = (obj.Wavid == LNObj);

                    if (obj.Wavid == LNObj)
                    {
                        // LNOBJタイプのロングノートの終端チェック
                        BMObject longbegin;
                        if (prevobj.TryGetValue(obj.Keyid, out longbegin))
                        {
                            // ロングノートの始点が見つかった場合
                            if (longbegin.Wavid == LNObj || longbegin.IsNotChannel1X2X())
                            {
                                ExceptionHandler.ThrowFormatError("LNOBJの始点が通常オブジェではありません。BMSファイルが誤っている可能性があります。");
                            }
                            longbegin.Terminal = obj;
                            prevobj.Remove(obj.Keyid);
                        }
                        else
                        {
                            ExceptionHandler.ThrowFormatError("LNOBJの始点が見つかりません。BMSファイルが誤っている可能性があります。");
                        }
                    }
                    else if (obj.IsChannel5X6X())
                    {
                        // 0x5X 0x6X の始点または終点チェック
                        BMObject longbegin;
                        if (prevobj.TryGetValue(obj.Keyid, out longbegin))
                        {
                            if (longbegin.IsChannel5X6X())
                            {
                                // objは終点
                                obj.IsLongNoteTerminal = true;
                                longbegin.IsLongNoteTerminal = false;
                                longbegin.Terminal = obj;
                                prevobj.Remove(obj.Keyid);
                            }
                            else
                            {
                                prevobj[obj.Keyid] = obj;
                            }
                        }
                        else
                        {
                            // objは始点
                            prevobj[obj.Keyid] = obj;
                        }
                    }
                    else
                    {
                        // 通常オブジェの場合
                        BMObject longbegin;
                        if (prevobj.TryGetValue(obj.Keyid, out longbegin))
                        {
                            if (longbegin.IsChannel5X6X())
                            {
                                ExceptionHandler.ThrowFormatError("ロングノートの終点が見つかりません。BMSファイルが誤っている可能性があります。たとえば、LN終端と不可視オブジェが重なっていませんか？");
                            }
                            else
                            {
                                prevobj[obj.Keyid] = obj;
                            }
                        }
                        else
                        {
                            prevobj[obj.Keyid] = obj;
                        }
                    }
                }
            }
            foreach (var kvpair in prevobj)
            {
                if (kvpair.Value.IsChannel5X6X())
                {
                    ExceptionHandler.ThrowFormatError("ロングノートの終点が見つかりません。BMSファイルが誤っている可能性があります。");
                }
            }
            #endregion

            #region オブジェの振り分け・トランスポートへの割り振り
            // AllBMObjectsの分類
            foreach (var obj in AllBMObjects)
            {
                if(obj.IsPlayable() && !obj.IsLongNoteTerminal) {
                    PlayableBMObjects.Add(obj);
                }

                if (obj.IsLongNoteTerminal)
                {
                    // ignore
                }
                else if (obj.IsSound())
                {
                    SoundBMObjects.Add(obj);
                }
                else if (obj.IsGraphic())
                {
                    GraphicBMObjects.Add(obj);
                }
                else
                {
                    // 制御オブジェ・LN終端・地雷等
                    if (obj.BMSChannel == 3)
                    {
                        // BPM定義
                        transp.AddTempoChange(obj.Measure, BMConvert.FromBase16(BMConvert.ToBase36(obj.Wavid)));
                    }
                    else if (obj.BMSChannel == 8)
                    {
                        // 拡張BPM定義
                        try
                        {
                            var tempo = Convert.ToDouble(BPMDefinitionList[obj.Wavid]);
                            transp.AddTempoChange(obj.Measure, tempo);
                        }
                        catch (Exception ex)
                        {
                            ExceptionHandler.ThrowFormatError("#BPM" + BMConvert.ToBase36(obj.Wavid) + "が定義されていないか、正しくありません。\n\n" + ex.ToString());
                        }
                    }
                    else if (obj.BMSChannel == 9)
                    {
                        // ストップシーケンス
                        try
                        {
                            var stop = Convert.ToDouble(StopDefinitionList[obj.Wavid]);
                            transp.AddStopSequence(obj.Measure, stop / 48.0);
                        }
                        catch(Exception ex)
                        {
                            ExceptionHandler.ThrowFormatError("#STOP" + BMConvert.ToBase36(obj.Wavid) + "が定義されていないか、正しくありません。\n\n" + ex.ToString());
                        }
                    }
                    else
                    {
                        if (!obj.IsLandmine())
                        {
                            ExceptionHandler.ThrowFormatWarning("不明なオブジェを検出しました。BMSChannel = " + obj.BMSChannel);
                        }
                        OtherBMObjects.Add(obj);
                    }
                }
            }
            #endregion

            #region BMSエディタによる読み込みではなく、BMSプレイヤーによる読み込みだった場合の項目調整
            if (!EditorMode)
            {
                // デフォルトテンポの設定
                transp.AddDefaultTempo(BPM);

                // 暗黙の副題
                if (Title != null && Subtitle == null)
                {
                    // http://hitkey.nekokan.dyndns.info/cmdsJP.htm#TITLE-IMPLICIT-SUBTITLE
                    var ImplicitSubtitleMatch = Regex.Match(Title, @"^(.*?)\s*(\-.+\-|～.+～|\(.+\)|\[.+\]|\<.+\>|"".+"")\z", RegexOptions.Compiled);
                    // Subtitleが空文字で定義されていた場合はどうするべきか？

                    if (ImplicitSubtitleMatch.Success)
                    {
                        Subtitle = ImplicitSubtitleMatch.Groups[2].Captures[0].Value;
                        Title = ImplicitSubtitleMatch.Groups[1].Captures[0].Value;
                    }
                }
                if (Artist != null && Subartist == null)
                {
                    var ImplicitSubartistMatch = Regex.Match(Artist, @"^(.*?)\s*(\-.+\-|～.+～|\(.+\)|\[.+\]|\<.+\>|"".+"")\z", RegexOptions.Compiled);
                    if (Subartist == null && ImplicitSubartistMatch.Success)
                    {
                        Subartist = ImplicitSubartistMatch.Groups[2].Captures[0].Value;
                        Artist = ImplicitSubartistMatch.Groups[1].Captures[0].Value;
                    }
                }

                if (BPM <= 0)
                {
                    ExceptionHandler.ThrowFormatError("BPM値が正しく指定されていません。");
                    BPM = 120;
                }

                // TOTAL値調整
                if (Total == null)
                {
                    ExceptionHandler.ThrowFormatWarning("Total値が指定されていません。");
                    Total = 260;
                }
                else if (Total < 80)
                {
                    ExceptionHandler.ThrowFormatWarning("Total値が不適切な値です。");
                    Total = 80;
                }

                // BeatToTempoChangeの準備
                transp.ArrangeTransport();

                foreach (var x in AllBMObjects)
                {
                    x.Beat = transp.MeasureToBeat(x.Measure);
                    x.Seconds = transp.BeatToSeconds(x.Beat);
                    x.Disp = transp.BeatToDisplacement(x.Beat);
                }
            }
            #endregion
        }

        // 秒数を横軸に取った時のテンポの中央値
        // position = 0.75とかにすると良さそう
        public double CalcTempoMedian(double position = 0.5)
        {
            if (AllBMObjects.Count == 0) return 120;

            double lasttime = 0;
            double lastBPM = BPM;
            double endtime = AllBMObjects[AllBMObjects.Count - 1].Seconds;

            var tempoAndTimespan = new List<Tuple<double, double>>();

            foreach (var m2t in transp.measureToTempoChange)
            {
                var nexttime = transp.MeasureToSeconds(m2t.Key);

                if (nexttime != lasttime)
                {
                    tempoAndTimespan.Add(Tuple.Create(lastBPM, nexttime - lasttime));
                }

                lasttime = nexttime;
                lastBPM = m2t.Value;
            }

            if (lasttime < endtime)
            {
                tempoAndTimespan.Add(Tuple.Create(lastBPM, endtime - lasttime));
            }

            // ソート
            tempoAndTimespan = tempoAndTimespan.OrderBy(x => x.Item1).ToList();

            // この文脈における総時間を計算
            double totaltime = tempoAndTimespan.Select(x => x.Item2).Sum();

            // 中央値を計算
            double currenttotal = 0;
            double finalBPM = 120;
            foreach (var TnTS in tempoAndTimespan)
            {
                currenttotal += TnTS.Item2;
                if (currenttotal >= totaltime * position)
                {
                    return TnTS.Item1;
                }
                finalBPM = TnTS.Item1;
            }
            return finalBPM;
                
        }
    }
}
