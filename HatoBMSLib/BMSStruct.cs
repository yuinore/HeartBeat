using HatoLib;
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

        public string ToFullPath(string filename)
        {
            if (DirectoryName == null) throw new Exception("Directoryメンバが設定されていません。");
            return Path.Combine(DirectoryName, filename);
        }

        //******************************************//
        // BMS Fields (未指定の場合は空文字ではなくnullが入る)
        public string Title;
        public string Subtitle;
        public string Artist;
        public string Subartist;
        public string Genre;

        public string Stagefile;
        public string BackBMP;

        // TODO: 初期値を定義しなかった場合に警告を出す
        public int Player = 1;
        public int Playlevel = 0;
        public int Rank = 3;
        public int Difficulty = 0;
        public double? Total;
        public int LNType = 1;
        public int? LNObj;

        public double BPM;
        //******************************************//
        // BMS Objects and Definition

        // #mmm02:の行が無い場合でも、デフォルトテンポの指定が入るため、空にはなりません。
        public Transport transp = new Transport();

        public Dictionary<string, string> BMSHeader = new Dictionary<string, string>();

        public List<BMObject> AllBMObjects = new List<BMObject>();

        // SoundBMObjects と GraphicBMObjects と OtherBMObjects は、
        // 積集合が空集合で、和集合がAllBMObjectsから####LN終端を除いたもの####に等しい。
        public List<BMObject> SoundBMObjects = new List<BMObject>();  // IsSound => true
        public List<BMObject> GraphicBMObjects = new List<BMObject>();  // IsGraphic => true
        public List<BMObject> OtherBMObjects = new List<BMObject>();

        public List<BMObject> PlayableBMObjects = new List<BMObject>();

        public Dictionary<int, string> WavDefinitionList = new Dictionary<int, string>();
        public Dictionary<int, string> BitmapDefinitionList = new Dictionary<int, string>();

        //******************************************//

        public BMSStruct(Stream str)
        {
            // TODO: Convertクラスでの例外の対応

            #region BMSファイルのパース
            var r = new StreamReader(str, Encoding.GetEncoding("Shift_JIS"));
            var linenumber = 0;

            while (!r.EndOfStream)
            {
                var line = r.ReadLine();
                linenumber++;

                // TODO: 行頭の空白文字の許容
                if (line.Length >= 1 && line[0] == '#')
                {
                    // コメントでない行
                    Match match;

                    // 小節長指定行にマッチ
                    var MatchMeasureLengthLine = new LazyMatch(line,
                        @"^#(\d\d\d)02:([0-9\.]+)$");

                    // オブジェ行
                    var MatchDataLine = new LazyMatch(line,
                        @"^#(\d\d\d)([0-9A-Za-z][0-9A-Za-z]):((?:[0-9A-Za-z][0-9A-Za-z])+)$");

                    // ヘッダ行
                    var MatchHeaderLine = new LazyMatch(line,
                        @"^#([^\d\s]\S*)\s*(.*)$");

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
                        var contents = match.Groups[3].Captures[0].Value;

                        Rational measureTime;
                        int partitions = contents.Length / 2;
                        for (int i = 0; i < partitions; i++)
                        {
                            int wavid = BMConvert.FromBase36(contents.Substring(i * 2, 2));
                            if (wavid != 0)
                            {
                                measureTime = measure + new Rational(i, partitions);

                                AllBMObjects.Add(new BMObject(bmsch, wavid, measureTime));
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
                        var MatchBMPXX = new LazyMatch(header, @"^BMP([0-9A-Za-z][0-9A-Za-z])$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                        if (MatchWavXX.Evaluate(out match))
                        {
                            // #WAVxx にマッチ
                            int wavid = BMConvert.FromBase36(match.Groups[1].Captures[0].Value);
                            WavDefinitionList.Add(wavid, val);
                        }
                        else if (MatchBMPXX.Evaluate(out match))
                        {
                            // #BMPxx にマッチ
                            int wavid = BMConvert.FromBase36(match.Groups[1].Captures[0].Value);
                            BitmapDefinitionList.Add(wavid, val);
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
                                case "STAGEFILE": Stagefile = val; break;
                                case "BACKBMP": BackBMP = val; break;
                                case "BPM":
                                    BPM = Convert.ToDouble(val);
                                    break;
                                case "PLAYER":
                                    Player = Convert.ToInt32(val);
                                    if (Player < 1 || Player > 4) throw new FormatException("#PLAYERの指定が不正です");
                                    break;
                                case "PLAYLEVEL":
                                    Playlevel = Convert.ToInt32(val);
                                    if (Playlevel < 0) throw new FormatException("#PLAYLEVELの指定が不正です");
                                    break;
                                case "RANK":
                                    Rank = Convert.ToInt32(val);
                                    if (Rank < 0 || Player > 4) throw new FormatException("#RANKの指定が不正です");
                                    break;
                                case "DIFFICULTY":
                                    Difficulty = Convert.ToInt32(val);
                                    if (Difficulty < 1 || Difficulty > 5) throw new FormatException("#DIFFICULTYの指定が不正です");
                                    break;
                                case "TOTAL":
                                    Total = Convert.ToDouble(val);
                                    if (Total < 0) throw new FormatException("#TOTALの指定が不正です");
                                    break;
                                case "LNTYPE":
                                    LNType = Convert.ToInt32(val);
                                    if (LNType < 1 || LNType > 2) throw new FormatException("#LNTYPEの指定が不正です");
                                    break;
                                case "LNOBJ":
                                    LNObj = BMConvert.FromBase36(val);
                                    if (LNObj < 1 || LNObj > 36 * 36 - 1) throw new FormatException("#LNOBJの指定が不正です");
                                    break;

                                default:
                                    BMSHeader.Add(header.ToUpper(), val);
                                    break;
                            }
                        }

                        #endregion
                    }
                    else
                    {
                        // ヘッダ行でもデータ行でもない（これはキャッチされることを想定している、多分）
                        // たとえば、 #01:12345 とか、 #7KEYS とか。
                        throw new FormatException("BMSから不適切な行が検出されました。BMSの読み込みを中断します。 行：" + linenumber + ", \"" + line + "\"");
                    }

                }
            }

            r.Close();
            #endregion

            AllBMObjects.Sort();

            #region LN終端の探索・設定
            Dictionary<int, BMObject> prevobj = new Dictionary<int, BMObject>();
            
            foreach (var obj in AllBMObjects)
            {
                // 暫定設定
                obj.IsLongNoteTerminal = false;

                if (obj.IsPlayable())
                {
                    // 暫定設定
                    obj.IsLongNoteTerminal = (obj.Wavid == LNObj);

                    int keyid = (obj.BMSChannel - 36) % 72;
                    if (obj.Wavid == LNObj)
                    {
                        // LNOBJタイプのロングノートの終端チェック
                        BMObject longbegin;
                        if (prevobj.TryGetValue(keyid, out longbegin))
                        {
                            // ロングノートの始点が見つかった場合
                            if (longbegin.Wavid == LNObj || longbegin.IsChannel5X6X())
                            {
                                throw new FormatException("LNOBJの始点が通常オブジェではありません。BMSファイルが誤っている可能性があります。");
                            }
                            longbegin.Terminal = obj;
                            prevobj.Remove(keyid);
                        }
                        else
                        {
                            throw new FormatException("LNOBJの始点が見つかりません。BMSファイルが誤っている可能性があります。");
                        }
                    }
                    else if (obj.IsChannel5X6X())
                    {
                        // 0x5X 0x6X の始点または終点チェック
                        BMObject longbegin;
                        if (prevobj.TryGetValue(keyid, out longbegin))
                        {
                            if (longbegin.IsChannel5X6X())
                            {
                                // objは終点
                                obj.IsLongNoteTerminal = true;
                                longbegin.IsLongNoteTerminal = false;
                                longbegin.Terminal = obj;
                                prevobj.Remove(keyid);
                            }
                            else
                            {
                                // objは始点
                                prevobj[keyid] = obj;
                            }
                        }
                        else
                        {
                            // objは始点
                            prevobj[keyid] = obj;
                        }
                    }
                    else
                    {
                        // 通常オブジェの場合
                        BMObject longbegin;
                        if (prevobj.TryGetValue(keyid, out longbegin))
                        {
                            if (longbegin.IsChannel5X6X())
                            {
                                throw new FormatException("ロングノートの終点が見つかりません。BMSファイルが誤っている可能性があります。");
                            }
                        }
                        prevobj[keyid] = obj;
                    }
                }
            }
            foreach (var kvpair in prevobj)
            {
                if (kvpair.Value.IsChannel5X6X())
                {
                    throw new FormatException("ロングノートの終点が見つかりません。BMSファイルが誤っている可能性があります。");
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

                if (obj.IsSound())
                {
                    SoundBMObjects.Add(obj);
                }
                else if (obj.IsGraphic())
                {
                    GraphicBMObjects.Add(obj);
                }
                else
                {
                    // TODO: 例外処理

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
                            var tempo = Convert.ToDouble(BMSHeader["BPM" + BMConvert.ToBase36(obj.Wavid)]);
                            transp.AddTempoChange(obj.Measure, tempo);
                        }
                        catch (Exception ex)
                        {
                            throw new FormatException("#BPM" + BMConvert.ToBase36(obj.Wavid) + "が定義されていないか、正しくありません。\n\n" + ex.ToString());
                        }
                    }
                    else if (obj.BMSChannel == 9)
                    {
                        // ストップシーケンス
                        try
                        {
                            var stop = Convert.ToDouble(BMSHeader["STOP" + BMConvert.ToBase36(obj.Wavid)]);
                            transp.AddStopSequence(obj.Measure, stop / 48.0);
                        }
                        catch(Exception ex)
                        {
                            throw new FormatException("#STOP" + BMConvert.ToBase36(obj.Wavid) + "が定義されていないか、正しくありません。\n\n" + ex.ToString());
                        }
                    }
                    else
                    {
                        OtherBMObjects.Add(obj);
                    }
                }
            }
            #endregion

            #region BMSエディタによる読み込みではなく、BMSプレイヤーによる読み込みだった場合の項目調整
            if (true)
            {
                // デフォルトテンポの設定
                transp.AddDefaultTempo(BPM);

                // 暗黙の副題
                if(Title!=null && Subtitle == null){
                    // http://hitkey.nekokan.dyndns.info/cmdsJP.htm#TITLE-IMPLICIT-SUBTITLE
                    var ImplicitSubtitleMatch = Regex.Match(Title, @"(.*)\s*(\-.+\-|～.+～|\(.+\)|\[.+\]|\<.+\>|" + "\"" + ".+" + "\"" + ")", RegexOptions.Compiled);
                    // Subtitleが空文字で定義されていた場合はどうするべきか？

                    if (ImplicitSubtitleMatch.Success)
                    {
                        Subtitle = ImplicitSubtitleMatch.Groups[2].Captures[0].Value;
                        Title = ImplicitSubtitleMatch.Groups[1].Captures[0].Value;
                    }
                }
                if (Artist != null && Subartist == null)
                {
                    var ImplicitSubartistMatch = Regex.Match(Artist, @"(.*)\s*(\-.+\-|～.+～|\(.+\)|\[.+\]|\<.+\>|" + "\"" + ".+" + "\"" + ")", RegexOptions.Compiled);
                    if (Subartist == null && ImplicitSubartistMatch.Success)
                    {
                        Subartist = ImplicitSubartistMatch.Groups[2].Captures[0].Value;
                        Artist = ImplicitSubartistMatch.Groups[1].Captures[0].Value;
                    }
                }

                if (BPM <= 0)
                {
                    Console.WriteLine("【警告】BPM値が正しく指定されていません。");

                    BPM = 120;
                }

                // TOTAL値調整
                if (Total == null)
                {
                    Console.WriteLine("【警告】Total値が指定されていません。");

                    Total = 260;
                }
                else if (Total < 80)
                {
                    Console.WriteLine("【警告】Total値が不適切な値です。");

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
