using Codeplex.Data;
using HatoLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * compressed patch の仕様です。
 * 
 *  fixed[8] variable;  // 固定長整数
 *  linked[8] variable;  // 可変長整数
 *  float variable;  // float値
 *  string variable;  // 文字列値
 *  [(n) ... ]  // n回の繰り返し
 *  
 *  //******** ここから仕様 ********
 *  fixed[8] version;
 *  linked[7] nBlocks;
 *  [(nBlocks)
 *      string name;
 *      linked[8] moduleId;
 *      fixed[1] hasChildren;
 *      [(hasChildren)
 *          linked[3] nChildren;
 *          [(nChildren)
 *              linked[7] childIndex;
 *              linked[3] port;
 *          ]
 *      ]
 *      fixed[1] hasCtrl;
 *      [(hasCtrl)
 *          linked[5] nCtrl;
 *          [(nCtrl)
 *              float ctrls;
 *          ]
 *      ]
 *      linked[7] pos;
 *      linked[8] gid;
 *      fixed[1] extension = 0;
 *  ]
 *  //******** ここまで ********
*/

namespace HatoDSP
{
    public static class PatchPacker
    {
        /// <summary>
        /// json形式(.hatp形式)のパッチから、float列形式のパッチに変換します。
        /// </summary>
        public static BitPacker Pack(string json)
        {
            BitPacker bp = new BitPacker();

            try
            {
                json = PatchReader.RemoveComments(json);

                dynamic dj = DynamicJson.Parse(json);

                if (!dj.IsArray) throw new PatchFormatException();

                Dictionary<string, int> NameToIndex = new Dictionary<string, int>();
                int nBlocks = 0;
                foreach (var x in (dynamic[])dj)
                {
                    string name = x.name;
                    NameToIndex.Add(name.ToLower(), nBlocks++);
                }

                bp.AddInteger(8, 1);  // version
                bp.AddLinkedInteger(7, nBlocks);  // total block count

                for (int i = 0; i < nBlocks; i++)
                {
                    dynamic x = ((dynamic[])dj)[i];

                    string name = x.name;
                    bp.AddString(name);

                    string moduleNameLower = ((string)x.module).ToLower();
                    int moduleId = 0;
                    if(name != "$synth") {
                        moduleId = ModuleList.Modules.First(y => y.NameLowerCase == moduleNameLower).Id;
                    }
                    bp.AddLinkedInteger(8, moduleId);

                    if (x.IsDefined("children"))
                    {
                        var children = (string[])x.children;  // このキャストって常に出来るの？

                        if (children.Length == 0)
                        {
                            bp.AddInteger(1, 0);
                        }
                        else
                        {
                            bp.AddInteger(1, 1);
                            bp.AddLinkedInteger(3, children.Length);

                            for (int j = 0; j < children.Length; j++)
                            {
                                string child = children[j];
                                int lidx = child.LastIndexOf(":");
                                if (lidx < 0) throw new PatchFormatException();
                                string childname = child.Substring(0, lidx);
                                int port = Convert.ToInt32(child.Substring(lidx + 1));
                                int childIndex = NameToIndex[childname.ToLower()];

                                bp.AddLinkedInteger(7, childIndex);
                                bp.AddLinkedInteger(3, port);
                            }
                        }
                    }
                    else
                    {
                        bp.AddInteger(1, 0);
                    }

                    if (x.IsDefined("ctrl"))
                    {
                        var ctrl = (double[])x.ctrl;

                        if (ctrl.Length == 0)
                        {
                            bp.AddInteger(1, 0);
                        }
                        else
                        {
                            bp.AddInteger(1, 1);
                            bp.AddLinkedInteger(5, ctrl.Length);

                            for (int j = 0; j < ctrl.Length; j++)
                            {
                                bp.AddFloat((float)ctrl[j]);
                            }
                        }
                    }
                    else
                    {
                        bp.AddInteger(1, 0);
                    }

                    if (x.IsDefined("pos"))
                    {
                        bp.AddLinkedInteger(7, (int)(x.pos + 0.5));
                    }
                    else
                    {
                        bp.AddLinkedInteger(7, 0);
                    }

                    if (x.IsDefined("gid"))
                    {
                        bp.AddLinkedInteger(8, (int)(x.gid + 0.5));
                    }
                    else
                    {
                        bp.AddLinkedInteger(8, 0);
                    }

                    bp.AddInteger(1, 0);  // extension flag
                }

                return bp;
            }
            catch (Exception ex)
            {
                throw new PatchFormatException(ex.ToString());
            }
        }

        private class PatchEntry
        {
            public string name { get; set; }
            public string module { get; set; }
            public string[] children { get; set; }
            public float[] ctrl { get; set; }
            public int pos { get; set; }
            public int gid { get; set; }

            public int[] childrenIds;  // プロパティではないので、DynamicJsonによってシリアライズされない（はず）
            public int[] ports;

            public PatchEntry()
            {
                this.gid = 1;
            }
        }

        /// <summary>
        /// float列形式のパッチから、json形式(.hatp形式)のパッチに変換します。
        /// </summary>
        public static string Unpack(float[] data)
        {
            try
            {
                BitUnpacker bup = new BitUnpacker(data);

                long version = bup.ReadInteger(8);
                if (version != 1) throw new PatchFormatException("パッチが読み込めませんでした。このソフトウェアをアップデートすると直るかもしれません。");

                long nBlocks = bup.ReadLinkedInteger(7);
                if (nBlocks >= Int32.MaxValue) throw new PatchFormatException();

                List<PatchEntry> entries = new List<PatchEntry>();

                List<string> BlockIndexToName = new List<string>();

                for (int i = 0; i < nBlocks; i++)
                {
                    PatchEntry entry = new PatchEntry();

                    entry.name = bup.ReadString();
                    BlockIndexToName.Add(entry.name);

                    if (entry.name == "$synth")
                    {
                        bup.ReadLinkedInteger(8);
                        entry.module = "";
                    }
                    else
                    {
                        entry.module = ModuleList.Modules[bup.ReadLinkedInteger(8)].Name;
                    }

                    long hasChildren = bup.ReadInteger(1);
                    if (hasChildren == 1)
                    {
                        int nChildren = (int)bup.ReadLinkedInteger(3);
                        var childrenIdList = new List<int>();
                        var portList = new List<int>();
                        for (int j = 0; j < nChildren; j++)
                        {
                            int childIndex = (int)bup.ReadLinkedInteger(7);
                            int port = (int)bup.ReadLinkedInteger(3);
                            childrenIdList.Add(childIndex);
                            portList.Add(port);
                        }
                        entry.childrenIds = childrenIdList.ToArray();
                        entry.ports = portList.ToArray();
                    }
                    else
                    {
                        entry.childrenIds = new int[] { };
                        entry.ports = new int[] { };
                    }

                    long hasCtrl = bup.ReadInteger(1);
                    if (hasCtrl == 1)
                    {
                        int nCtrl = (int)bup.ReadLinkedInteger(5);
                        var ctrlList = new List<float>();
                        for (int j = 0; j < nCtrl; j++)
                        {
                            float ctrl = bup.ReadFloat();
                            ctrlList.Add(ctrl);
                        }
                        entry.ctrl = ctrlList.ToArray();
                    }
                    else
                    {
                        entry.ctrl = new float[] { };
                    }

                    entry.pos = (int)bup.ReadLinkedInteger(7);
                    entry.gid = (int)bup.ReadLinkedInteger(8);

                    long extension = bup.ReadInteger(1);
                    if (extension == 1) throw new PatchFormatException("パッチが読み込めませんでした。このソフトウェアをアップデートすると直るかもしれません。");

                    entries.Add(entry);
                }

                for (int i = 0; i < nBlocks; i++)
                {
                    List<string> children = new List<string>();

                    for (int j = 0; j < entries[i].childrenIds.Length; j++)
                    {
                        string name = BlockIndexToName[(int)entries[i].childrenIds[j]];  // 結局intにするんじゃないですか・・・！！
                        long port = entries[i].ports[j];

                        children.Add(name + ":" + port);
                    }

                    entries[i].children = children.ToArray();
                }

                string json = DynamicJson.Serialize(entries);

                // jsonを整形
                dynamic parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                json = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);

                return json;
            }
            catch (Exception ex)
            {
                // catchが雑すぎる・・・！！
                throw new PatchFormatException(ex.ToString());
            }
        }
    }
}
