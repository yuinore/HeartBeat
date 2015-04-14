using Codeplex.Data;
using System;
using System.Collections.Generic;
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
        public PatchReader(string json)
        {
            try
            {
                // jsonではコメント使えないんですか！？！？
                //json = Regex.Replace(json, @"//.*$", "", RegexOptions.Multiline);
                //json = Regex.Replace(json, @"/\*.*\*/", "");  // これだと /* /* */ もアレしてしまう気がする
                //json = Regex.Replace(json, @"/\*.*?\*/", "");  // これだと /* // */ もアレしてしまう
                json = Regex.Replace(json, @"(/\*.*?\*/|//.*?$)", "", RegexOptions.Multiline | RegexOptions.Singleline);
                // ↑これだと文字列定数中のコメントも削除されそう・・・
                // これだと単独の/*はコメント化されないですが構いませんよね？

                dynamic dj = DynamicJson.Parse(json);

                if (!dj.IsArray) throw new PatchFormatException();

                foreach (var x in (dynamic[])dj)
                {
                    string name = x.name;

                    if (name == "$synth")
                    {
                        dynamic[] cldrn = (dynamic[])x.children;
                        if (cldrn.Length != 1) new NotImplementedException("あー");

                        root = (string)cldrn[0];
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
                    cells[x.Key].AssignChildren(x.Value.Select(y => cells[y]).ToArray());
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