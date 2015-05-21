using Codeplex.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class PatchReader
    {
        Dictionary<string, CellTree> cells = new Dictionary<string, CellTree>();
        Dictionary<string, string[]> children = new Dictionary<string, string[]>();
        Dictionary<string, double[]> ctrl = new Dictionary<string, double[]>();  // TODO: エクスプレッションの対応
        string root = null;

        public CellTree Root
        {
            get
            {
                return cells[root];
            }
        }

        public static string RemoveComments(string jsonWithComments)
        {
            // jsonではコメント使えないんですか！？！？
            //json = Regex.Replace(json, @"//.*$", "", RegexOptions.Multiline);
            //json = Regex.Replace(json, @"/\*.*\*/", "");  // これだと /* /* */ もアレしてしまう気がする
            //json = Regex.Replace(json, @"/\*.*?\*/", "");  // これだと /* // */ もアレしてしまう
            //return Regex.Replace(jsonWithComments, @"(/\*.*?\*/|//.*?$)", "", RegexOptions.Multiline | RegexOptions.Singleline);
            // FIXME: ↑これだと文字列定数中のコメントも削除されそう・・・

            // ^((?:[^\"]*?\"(\\\"|[^\"\\])*?\")*?[^\"]*?)(/\*.*?\*/|//.*?\r\n)
            string pattern = @"^((?:[^\""]*?\""(?:\\\""|[^\""\\])*?\"")*?[^\""]*?)(/\*.*?\*/|//.*?(\r\n|\Z))";  // 改行文字は\r\n
            // これでどうでしょうか
            // 処理時間的な意味での改善の余地はあるかもしれない。

            int recursionLimit = 1000;

            while (true)
            {
                string replaced = Regex.Replace(jsonWithComments, pattern, "$1\r\n", RegexOptions.Singleline | RegexOptions.Compiled);  // 空白文字に置換してもよい
                // jsonは改行と空白を区別しない

                if (jsonWithComments == replaced) break;

                jsonWithComments = replaced;

                if (--recursionLimit <= 0) break;
            }

            Debug.Assert(recursionLimit > 0, "正規表現は難しいよねｗ");
            return jsonWithComments;
        }

        public PatchReader(string json)
        {
            try
            {
                json = RemoveComments(json);

                dynamic dj = DynamicJson.Parse(json);

                if (!dj.IsArray) throw new PatchFormatException();

                foreach (var x in (dynamic[])dj)
                {
                    string name = x.name;

                    if (name == "$synth")
                    {
                        dynamic[] cldrn = (dynamic[])x.children;
                        if (cldrn.Length != 1) new NotImplementedException("あー");

                        string cld = (string)cldrn[0];
                        if (cld.Substring(cld.LastIndexOf(":") + 1) != "0") throw new Exception("あー");
                        root = cld.Substring(0, cld.LastIndexOf(":"));
                    }
                    else
                    {
                        cells[name] = new CellTree(x.name, x.module);

                        // TODO: childrenのポート番号の指定
                        if (x.IsDefined("children"))
                        {
                            children[name] = (string[])x.children;
                        }
                        if (x.IsDefined("ctrl"))
                        {
                            ctrl[name] = (double[])x.ctrl;
                        }
                    }
                }

                foreach (var x in children)
                {
                    cells[x.Key].AddChildren(x.Value.Select(y => {
                        // "name:port" の形で指定する。portの省略は多分できない。
                        int idx = y.LastIndexOf(":");
                        if (idx == -1) throw new Exception("child は name:port の形で指定して下さい。portの省略はできません。");

                        string name = y.Substring(0, idx);
                        int port = Int32.Parse(y.Substring(idx + 1));
                        return new CellWire(cells[name], port);
                    }).ToArray());
                }

                foreach (var x in ctrl)
                {
                    cells[x.Key].AssignControllers(x.Value.Select(y => new CellParameterValue((float)y)).ToArray());
                }

                if (root == null) throw new PatchFormatException();
            }
            catch (System.Xml.XmlException ex)
            {
                throw new PatchFormatException(ex.ToString());
            }
        }
    }
}