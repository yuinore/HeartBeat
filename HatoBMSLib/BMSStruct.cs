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

        // #mmm02:の行が無い場合でも、デフォルトテンポの指定が入るため、空にはなりません。
        public Transport transp = new Transport();

        public Dictionary<string, string> BMSHeader = new Dictionary<string, string>();

        public List<BMObject> AllBMObjects = new List<BMObject>();

        public List<BMObject> SoundBMObjects = new List<BMObject>();  // IsSound => true
        public List<BMObject> GraphicBMObjects = new List<BMObject>();  // IsGraphic => true
        public List<BMObject> OtherBMObjects = new List<BMObject>();

        public Dictionary<int, string> WavDefinitionList = new Dictionary<int, string>();

        public BMSStruct(Stream str)
        {
            // TODO: Convertクラスでの例外の対応

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
                    var MatchMeasureLengthLine = Regex.Match(line, @"^#(\d\d\d)02:([0-9\.]+)$");

                    if (MatchMeasureLengthLine.Success)
                    {
                        #region 小節長指定行にマッチ
                        int measure = Convert.ToInt32(MatchMeasureLengthLine.Groups[1].Captures[0].Value);
                        var contents = Convert.ToDouble(MatchMeasureLengthLine.Groups[2].Captures[0].Value);

                        transp.AddSignature(measure, contents);

                        #endregion
                    }
                    else
                    {
                        var MatchDataLine = Regex.Match(line, @"^#(\d\d\d)([0-9A-Za-z][0-9A-Za-z]):((?:[0-9A-Za-z][0-9A-Za-z])+)$");  // ここに消えてはならないデリゲートが！？
                        //var MatchDataLine = Regex.Match(line, @"^#(\d\d\d)02:([0-9\.]+)$");
                        if (MatchDataLine.Success)
                        {
                            #region データ行にマッチ
                            int measure = Convert.ToInt32(MatchDataLine.Groups[1].Captures[0].Value);
                            int bmsch = BMConvert.FromBase36(MatchDataLine.Groups[2].Captures[0].Value);
                            var contents = MatchDataLine.Groups[3].Captures[0].Value;

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
                            #endregion
                            }
                        }
                    
                        else
                        {
                            var MatchHeaderLine = Regex.Match(line, @"^#([^\d\s]\S*)\s*(.*)$");

                            if (MatchHeaderLine.Success)
                            {
                                #region ヘッダ行にマッチ（内容があるかどうかは問わない）
                                var header = MatchHeaderLine.Groups[1].Captures[0].Value;
                                var val = MatchHeaderLine.Groups[2].Captures[0].Value;

                                var MatchWavXX = Regex.Match(header, @"^WAV([0-9A-Za-z][0-9A-Za-z])$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                                if (MatchWavXX.Success)
                                {
                                    // #WAVxx にマッチ
                                    int wavid = BMConvert.FromBase36(MatchWavXX.Groups[1].Captures[0].Value);
                                    WavDefinitionList.Add(wavid, val);
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
                                throw new FormatException("BMSから不適切な行が検出されました。BMSの読み込みを中断します。 行：" + linenumber + ", \"" + line + "\"");
                            }
                        }
                    }
                }
            }
            
            // 調整処理
            AllBMObjects.Sort();


            // AllBMObjectsの分類
            foreach (var obj in AllBMObjects)
            {
                obj.IsLongNoteTerminal = (obj.Wavid == LNObj);

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
                    // 制御オブジェ・LN終端・地雷等
                    if (obj.BMSChannel == 3)
                    {
                        transp.AddTempoChange(obj.Measure, BMConvert.FromBase16(BMConvert.ToBase36(obj.Wavid)));
                    }
                    else if (obj.BMSChannel == 8)
                    {
                        var tempo = Convert.ToDouble(BMSHeader["BPM" + BMConvert.ToBase36(obj.Wavid)]);
                        transp.AddTempoChange(obj.Measure, tempo);
                    }
                    else
                    {
                        OtherBMObjects.Add(obj);
                    }
                }
            }

            // BMSエディタによる読み込みではなく、BMSプレイヤーによる読み込みだった場合の項目調整
            if (true)
            {
                // デフォルトテンポの設定
                double initialTempo;
                if (!transp.measureToTempoChange.TryGetValue(0, out initialTempo))
                {
                    transp.AddTempoChange(0, BPM);
                }

                // 暗黙の副題
                if(Title!=null && Subtitle == null){
                    // http://hitkey.nekokan.dyndns.info/cmdsJP.htm#TITLE-IMPLICIT-SUBTITLE
                    var ImplicitSubtitleMatch = Regex.Match(Title, @"(.*)\s*(\-.+\-|～.+～|\(.+\)|\[.+\]|\<.+\>|" + "\"" + ".+" + "\"" + ")", RegexOptions.Compiled);
                    // Subtitleが空文字で定義されていた場合はどうするべきか？

                    if ( ImplicitSubtitleMatch.Success)
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


                foreach (var x in SoundBMObjects.Concat(GraphicBMObjects).Concat(OtherBMObjects))
                {
                    x.Beat = transp.MeasureToBeat(x.Measure);
                    x.Seconds = transp.BeatToSeconds(x.Beat);
                }

            }
            
            r.Close();
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
