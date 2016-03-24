using HatoBMSLib;
using HatoLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnzuBMSDiff
{
    class BMSDiffCheck
    {
        readonly double ThresholdSeconds = 1.0e-5;  // 時間差判定

        public static bool CheckForGraphics = true;
        public static bool CheckForSounds = true;
        public static bool CheckForMetaInfo = true;
        
        public int MyCompare(BMSStruct X, BMSStruct Y, BMObject x, BMObject y)
        {
            //compare in Measure
            /*
            if (x.Measure != y.Measure)
            {
                return x.Measure > y.Measure ? 1 : -1;
            }
            if (X.WavDefinitionList.GetValueOrDefault(x.Wavid) != Y.WavDefinitionList.GetValueOrDefault(y.Wavid))
            {
                return (X.WavDefinitionList.GetValueOrDefault(x.Wavid) ?? "").CompareTo(Y.WavDefinitionList.GetValueOrDefault(y.Wavid) ?? "");
            }
             */
            if (Math.Abs(x.Seconds - y.Seconds) >= ThresholdSeconds)
            {
                return x.Seconds > y.Seconds ? 1 : -1;
            }
            if (X.WavDefinitionList.GetValueOrDefault(x.Wavid) != Y.WavDefinitionList.GetValueOrDefault(y.Wavid))
            {
                return (X.WavDefinitionList.GetValueOrDefault(x.Wavid) ?? "").CompareTo(Y.WavDefinitionList.GetValueOrDefault(y.Wavid) ?? "");
            }
            return 0;
        }

        public int MyCompareGraphic(BMSStruct X, BMSStruct Y, BMObject x, BMObject y)
        {
            if (Math.Abs(x.Seconds - y.Seconds) >= ThresholdSeconds)
            {
                return x.Seconds > y.Seconds ? 1 : -1;
            }
            if (X.BitmapDefinitionList.GetValueOrDefault(x.Wavid) != Y.BitmapDefinitionList.GetValueOrDefault(y.Wavid))
            {
                return (X.BitmapDefinitionList.GetValueOrDefault(x.Wavid) ?? "").CompareTo(Y.BitmapDefinitionList.GetValueOrDefault(y.Wavid) ?? "");
            }
            return 0;
        }


        public String Diff(BMSStruct X, BMSStruct Y, out int errorsCount)
        {
            var dmyobj = new BMObject(01, 01, new HatoLib.Rational(99999));
            dmyobj.Seconds = 1.0e8;
            int i, j;
            int errcnt = 0;
            StringSuruyatuSafe s = new StringSuruyatuSafe();
            string header = "";

            //***************************** SOUNDS CHECK **************************************
            if (CheckForSounds)
            {
                List<BMObject> x2 = X.SoundBMObjects.Where(x => !x.IsInvisible() && X.WavDefinitionList.ContainsKey(x.Wavid))
                    .OrderBy(x => X.WavDefinitionList[x.Wavid]).OrderBy(x => x.Beat).Concat(new BMObject[] { dmyobj }).ToList();
                List<BMObject> y2 = Y.SoundBMObjects.Where(x => !x.IsInvisible() && Y.WavDefinitionList.ContainsKey(x.Wavid))
                    .OrderBy(x => Y.WavDefinitionList[x.Wavid]).OrderBy(x => x.Beat).Concat(new BMObject[] { dmyobj }).ToList();
                
                s += "X : \"" + X.BMSFilePath + "\" " + (x2.Count - 1) + " objs (無音除く)\r\n";
                s += "Y : \"" + Y.BMSFilePath + "\" " + (y2.Count - 1) + " objs (無音除く)\r\n\r\n";

                header = "******** duplicate check ********\r\nbar\tposition\twavid\r\n";

                for (i = 0; i < x2.Count - 1; i++)
                {
                    //if (MyCompare(X, Y, x2[i], x2[i + 1]) == 0)
                    if (MyCompare(X, X, x2[i], x2[i + 1]) == 0)
                    {
                        s += header + x2[i] + " is duplicated in X. (" + X.WavDefinitionList.GetValueOrDefault(x2[i].Wavid) + ")\r\n";
                        header = "";
                        errcnt++;
                        x2.RemoveAt(i + 1);
                        i--;
                    }
                }
                for (i = 0; i < y2.Count - 1; i++)
                {
                    if (MyCompare(Y, Y, y2[i], y2[i + 1]) == 0)
                    {
                        s += header + y2[i] + " is duplicated in Y. (" + Y.WavDefinitionList.GetValueOrDefault(y2[i].Wavid) + ")\r\n";
                        header = "";
                        errcnt++;
                        y2.RemoveAt(i + 1);
                        i--;
                    }
                }

                header = "\r\n******** difference check ********\r\nbar\tposition\twavid\r\n";

                for (i = j = 0; i < x2.Count - 1 || j < y2.Count - 1;)
                {
                    if (MyCompare(X, Y, x2[i], y2[j]) == 0)
                    {
                        i++; j++;
                    }
                    else if (MyCompare(X, Y, x2[i], y2[j]) < 0)  // x2[i] < y2[j]
                    {
                        s += header + x2[i] + " is missing in Y. (" + X.WavDefinitionList.GetValueOrDefault(x2[i].Wavid) + ")\r\n";
                        header = "";
                        errcnt++;
                        i++;
                    }
                    else  // x2[i] > y2[j]
                    {
                        s += header + y2[j] + " is missing in X. (" + Y.WavDefinitionList.GetValueOrDefault(y2[j].Wavid) + ")\r\n";
                        header = "";
                        errcnt++;
                        j++;
                    }
                }
            }

            //***************************** GRAPHICS CHECK **************************************
            if (CheckForGraphics)
            {
                List<BMObject> x2 = X.GraphicBMObjects.Where(x => X.BitmapDefinitionList.ContainsKey(x.Wavid))
                .OrderBy(x => X.BitmapDefinitionList[x.Wavid]).OrderBy(x => x.Beat).Concat(new BMObject[] { dmyobj }).ToList();
                List<BMObject> y2 = Y.GraphicBMObjects.Where(x => Y.BitmapDefinitionList.ContainsKey(x.Wavid))
                    .OrderBy(x => Y.BitmapDefinitionList[x.Wavid]).OrderBy(x => x.Beat).Concat(new BMObject[] { dmyobj }).ToList();

                header = "\r\n******** difference (graphic) check ********\r\nbar\tposition\twavid\r\n";

                for (i = j = 0; i < x2.Count - 1 || j < y2.Count - 1;)
                {
                    if (MyCompareGraphic(X, Y, x2[i], y2[j]) == 0)
                    {
                        i++; j++;
                    }
                    else if (MyCompareGraphic(X, Y, x2[i], y2[j]) < 0)  // x2[i] < y2[j]
                    {
                        s += header + x2[i] + " is missing in Y. (" + X.BitmapDefinitionList.GetValueOrDefault(x2[i].Wavid) + ")\r\n";
                        header = "";
                        errcnt++;
                        i++;
                    }
                    else  // x2[i] > y2[j]
                    {
                        s += header + y2[j] + " is missing in X. (" + Y.BitmapDefinitionList.GetValueOrDefault(y2[j].Wavid) + ")\r\n";
                        header = "";
                        errcnt++;
                        j++;
                    }
                }
                if (X.Stagefile != Y.Stagefile)
                {
                    s += header + "Stagefile Entries are different. (X : \"" + X.Stagefile + "\", Y : \"" + Y.Stagefile + "\")\r\n";
                    header = "";
                    errcnt++;
                }
                if (X.BackBMP != Y.BackBMP)
                {
                    s += header + "BackBMP Entries are different. (X : \"" + X.BackBMP + "\", Y : \"" + Y.BackBMP + "\")\r\n";
                    header = "";
                    errcnt++;
                }
                if (X.Banner != Y.Banner)
                {
                    s += header + "Banner Entries are different. (X : \"" + X.Banner + "\", Y : \"" + Y.Banner + "\")\r\n";
                    header = "";
                    errcnt++;
                }
            }

            //***************************** META CHECK **************************************
            if (CheckForMetaInfo)
            {
                header = "\r\n******** meta info check ********\r\n";

                if (X.Title != Y.Title)
                {
                    s += header + "Title Entries are different. (X : \"" + X.Title + "\", Y : \"" + Y.Title + "\")\r\n";
                    header = "";
                    errcnt++;
                }
                if (X.Artist != Y.Artist)
                {
                    s += header + "Artist Entries are different. (X : \"" + X.Artist + "\", Y : \"" + Y.Artist + "\")\r\n";
                    header = "";
                    errcnt++;
                }
                if (X.Genre != Y.Genre)
                {
                    s += header + "Genre Entries are different. (X : \"" + X.Genre + "\", Y : \"" + Y.Genre + "\")\r\n";
                    header = "";
                    errcnt++;
                }
                if (X.Total == null)
                {
                    s += header + "Total is not set. (X)\r\n";
                    header = "";
                    errcnt++;
                }
                if (Y.Total == null)
                {
                    s += header + "Total is not set. (Y)\r\n";
                    header = "";
                    errcnt++;
                }
            }

            s += "\r\n******** " + errcnt + " errors found. ********\r\n\r\n";
            s += "******** end of report ********\r\n";

            errorsCount = errcnt;

            return s.ToString();
        }

    }
}
