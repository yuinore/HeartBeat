using HatoBMSLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Simamu
{
    static class BMSStructWriterTestClass
    {
        public static void TestOneFile(string path, Window form)
        {
            var bms2 = new BMSStruct(path, true);

            bms2.Export(path + "_written-by-heartbeat.bms");

            if (bms2.Message != "")
            {
                MessageBox.Show(form, bms2.Message);
            }

            var lines1 = File.ReadAllLines(path, Encoding.GetEncoding("Shift_JIS"));
            var lines2 = File.ReadAllLines(path + "_written-by-heartbeat.bms", Encoding.GetEncoding("Shift_JIS"));

            Console.WriteLine(
                lines1.Zip(lines2, (x, y) => { if (x != y) { Console.WriteLine("error! line1:" + x + ", line2:" + y); return 1; } return 0; }).ToArray().Sum()
                );

            return;
        }

        public static void Search(string directory)
        {
            foreach (var dir2 in Directory.GetDirectories(directory))
            {
                Search(dir2);
            }

            foreach (var file in Directory.GetFiles(directory))
            {
                // どうやらiBMSCのバージョンか何かの違いによって、連続する空行の数が違うことがあるみたいですね・・・

                var ext = Path.GetExtension(file).ToLower();

                if (ext != ".bms" && ext != ".bme" && ext != ".bml" && ext != ".pms")
                {
                    continue;
                }

                // \r\n\r\n#STAGEFILE を含むならiBMSCで編集したファイルである
                // *---------------------- EXPANSION FIELD を含むならiBMSCで編集したファイルである
                // #STAGEFILE -> #TOTAL の順に両者が出現するならiBMSCで編集したファイルである

                {
                    var txt = File.ReadAllText(file, Encoding.GetEncoding("Shift_JIS"));

                    if (txt.IndexOf("\r\n\r\n#STAGEFILE") < 0)
                    {
                        if (txt.IndexOf("\r\n*---------------------- EXPANSION FIELD") < 0)
                        {
                            int stage = txt.IndexOf("\r\n#STAGEFILE");
                            int total = txt.IndexOf("\r\n#TOTAL");

                            if (stage < 0 || total < 0 || total < stage)
                            {
                                // iBMSCで編集したファイルではない
                                continue;
                            }
                        }
                    }

                    if (txt.IndexOf("\r\n#RANDOM") >= 0)
                    {
                        continue;
                    }
                }

                var bms2 = new BMSStruct(file, true);

                bms2.Export(file + "_written-by-heartbeat.bms");

                if (bms2.Message != "")
                {
                    Console.WriteLine();
                    Console.WriteLine("Message at " + file + " end.");
                    //Console.WriteLine(bms2.Message);
                    Console.WriteLine();
                }


                var lines1 = Regex.Replace(
                    File.ReadAllText(file, Encoding.GetEncoding("Shift_JIS")), @"\r\n(\r\n)+", "\r\n\r\n")
                    .Split(new[] { "\r\n" }, StringSplitOptions.None);

                var lines2 = Regex.Replace(
                    File.ReadAllText(file + "_written-by-heartbeat.bms", Encoding.GetEncoding("Shift_JIS")), @"\r\n(\r\n)+", "\r\n\r\n")
                    .Split(new[] { "\r\n" }, StringSplitOptions.None);

                /*var lines1 = File.ReadAllLines(file, Encoding.GetEncoding("Shift_JIS"));
                var lines2 = File.ReadAllLines(file + "_written-by-heartbeat.bms", Encoding.GetEncoding("Shift_JIS"));
                */

                var result = lines1.Zip(lines2, (x, y) =>
                {
                    if (x != y)
                    {
                        //Console.WriteLine("error! line1:" + x + ", line2:" + y);
                        return 1;
                    }
                    return 0;
                }).ToArray().Sum();

                if (result >= 1)
                {
                    Console.WriteLine();
                    Console.WriteLine(result + " error(s) in " + file);
                }
                else
                {
                    Console.Write("!");
                }
            }
        }

    }
}
