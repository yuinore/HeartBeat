using Codeplex.Data;
using HatoDSP;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HatoSynthGUI
{
    static class PatchIO
    {
        private class PatchEntry
        {
            public string name { get; set; }
            public string module { get; set; }
            public string[] children { get; set; }
            public float[] ctrl { get; set; }
            public int pos { get; set; }
            public int gid { get; set; }

            public PatchEntry()
            {
                this.gid = 1;
            }

            public PatchEntry(dynamic obj)
            {
                this.name = obj.name;

                if (obj.IsDefined("module"))
                {
                    this.module = obj.module;
                }

                if (obj.IsDefined("children"))
                {
                    this.children = obj.children;
                }

                if (obj.IsDefined("ctrl"))
                {
                    this.ctrl = ((double[])obj.ctrl).Select(y => (float)y).ToArray();
                }

                if (obj.IsDefined("pos"))
                {
                    this.pos = (int)(double)obj.pos;  // posが定義されていない場合はGUI上に読み込むことができない。
                }

                if (obj.IsDefined("gid"))
                {
                    this.gid = (int)(double)obj.gid;
                }
                else
                {
                    this.gid = 1;
                }
            }

            public int SetPos(int x, int y)  // どうしてこうなった？
            {
                if (x > 0xFFF || y > 0xFFF || x < 0 || y < 0) throw new Exception();

                return pos = (
                    ((x & 0x007) << 0) |
                    ((x & 0x038) << 3) |
                    ((x & 0x1C0) << 6) |
                    ((x & 0xD00) << 9) |
                    ((y & 0x007) << 3) |
                    ((y & 0x038) << 6) |
                    ((y & 0x1C0) << 9) |
                    ((y & 0xD00) << 12));
            }

            public void GetPos(out int x, out int y)
            {
                x = (
                    ((pos & 0x000007) >> 0) |
                    ((pos & 0x0001C0) >> 3) |
                    ((pos & 0x007000) >> 6) |
                    ((pos & 0x1C0000) >> 9));

                y = (
                    ((pos & 0x000038) >> 3) |
                    ((pos & 0x000D00) >> 6) |
                    ((pos & 0x038000) >> 9) |
                    ((pos & 0xD00000) >> 12));
            }
        }

        /// <summary>
        /// GUI上で構成されているパッチをシリアライズします。
        /// シリアライズできなかった場合はnullを返します。(FIXME:)
        /// </summary>
        public static string Serialize(Size TableSize, BlockTableManager btable, List<ArrowSummary> arrows)
        {
            PatchEntry[,] cells = new PatchEntry[TableSize.Height, TableSize.Width];
            for (int y = 0; y < TableSize.Height; y++)
            {
                for (int x = 0; x < TableSize.Width; x++)
                {
                    BlockPatch preset;

                    if (btable.TryGetBlockPatch(x, y, out preset))
                    {
                        // DynamicJsonを編集しようかと思ったけど難しすぎる・・・
                        // DynamicJsonを入れ子にできれば捗るのだけれど、
                        // そもそも遅延評価になっていないという罠。

                        PatchEntry cell = new PatchEntry
                        {
                            name = preset.Name,
                            module = preset.ModuleName,
                            ctrl = new float[0] { },  // 後から追加できない・・・
                            children = new string[0] { },
                            gid = preset.GraphicId
                        };

                        if (preset.Ctrl != null)
                        {
                            /////////////////////////////////////////////////////
                            cell.ctrl = preset.Ctrl;
                        }

                        cell.SetPos(x, y);
                        cells[y, x] = cell;
                    }
                }
            }

            // 最下段以外の矢印
            for (int arrowId = 0; arrowId < arrows.Count; arrowId++)
            {
                PatchEntry src = null, dst = null;
                int port = 0;
                ArrowSummary sm = arrows[arrowId];

                if (sm.pos2y == TableSize.Height) continue;  // 最下段の矢印だった場合

                switch (sm.direction)
                {
                    case ArrowDirection.Right:
                    case ArrowDirection.Down:
                    case ArrowDirection.RightAlt:
                    case ArrowDirection.DownAlt:
                        src = cells[sm.pos1y, sm.pos1x];  // 順方向
                        dst = cells[sm.pos2y, sm.pos2x];
                        break;
                    case ArrowDirection.Left:
                    case ArrowDirection.Up:
                    case ArrowDirection.LeftAlt:
                    case ArrowDirection.UpAlt:
                        src = cells[sm.pos2y, sm.pos2x];  // 逆方向
                        dst = cells[sm.pos1y, sm.pos1x];
                        break;
                }

                switch (sm.direction)
                {
                    case ArrowDirection.Right:
                    case ArrowDirection.Down:
                    case ArrowDirection.Left:
                    case ArrowDirection.Up:
                        port = 0;
                        break;
                    case ArrowDirection.RightAlt:
                    case ArrowDirection.DownAlt:
                    case ArrowDirection.LeftAlt:
                    case ArrowDirection.UpAlt:
                        port = 1;
                        break;
                }

                if (src != null && dst != null)
                {
                    // TODO: 空の配列は書き出さないようにできる？
                    IEnumerable<string> arr = dst.children;
                    arr = arr.Concat(new string[] { src.name + ":" + port });
                    dst.children = arr.ToArray();
                }
            }

            PatchEntry start = null;

            // 最下段の矢印について
            for (int arrowId = 0; arrowId < arrows.Count; arrowId++)
            {
                ArrowSummary sm = arrows[arrowId];

                if (sm.pos2y != TableSize.Height) continue;  // 最下段の矢印ではなかった場合

                if ((sm.direction == ArrowDirection.Down || sm.direction == ArrowDirection.DownAlt)
                    && cells[sm.pos1y, sm.pos1x] != null)
                {
                    start = new PatchEntry
                    {
                        name = "$synth",
                        module = "",
                        children = new string[] { cells[sm.pos1y, sm.pos1x].name + ":0" },
                        ctrl = new float[0] { }
                    };
                }
            }

            dynamic json = DynamicJson.Parse("[]");

            // jsonに追加
            foreach (var cell in cells)
            {
                if (cell != null)
                {
                    json[((dynamic[])json).Length] = cell;  // 【オブジェクトはこの時点でシリアライズされてしまう（遅延評価ではない）】
                }
            }

            if (start != null)
            {
                json[((dynamic[])json).Length] = start;

                //Task.Run(() => MessageBox.Show("serialize start : " + DateTime.Now.Second + ", " + DateTime.Now.Millisecond));

                string str = json.ToString();

                // jsonを整形
                dynamic parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(str);
                str = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);

                return str;
            }
            else
            {
                return null;
            }
        }

        public static void Deserialize(
            string patch, 
            BlockTableManager btable,
            List<ArrowSummary> arrows,
            PictureBoxGenerator pBoxGen,
            Action<PictureBox> CellBlockArrangementCallback)
        {
            var cellNameToCellBlock = new Dictionary<string, CellBlock>();
            var children = new Dictionary<string, string[]>();

            try
            {
                PatchEntry[] entries = null;

                {
                    // json から PatchEntry[] を読み込む

                    string json = PatchReader.RemoveComments(patch);

                    dynamic dj = DynamicJson.Parse(json);

                    if (!dj.IsArray) throw new PatchFormatException();

                    entries = ((dynamic[])dj).Select(obj => new PatchEntry(obj)).ToArray();
                }

                foreach (var entry in entries)
                {
                    if (entry.name == "$synth")
                    {
                        if (entry.children.Length != 1) new NotImplementedException("あー");

                        string cld = entry.children[0];
                        int idx = cld.LastIndexOf(":");
                        if (idx < 0 || cld.Substring(idx + 1) != "0") throw new Exception("あー");

                        children.Add("$synth", new string[] { cld });
                    }
                    else
                    {
                        var cel = new CellBlock();
                        cel.bpatch = new BlockPatch(
                                entry.gid,
                                entry.module,
                                entry.name,
                                entry.ctrl
                            );
                        entry.GetPos(out cel.x, out cel.y);

                        cel.pBox = pBoxGen.GenerateCellBlock(cel.bpatch.GraphicId, cel.x, cel.y);

                        btable.Add(cel.pBox, cel.x, cel.y, cel.bpatch);

                        // イベントハンドラ・右クリックメニューを設定し、親コンテナにPictureBoxを追加します。
                        CellBlockArrangementCallback(cel.pBox);

                        children.Add(entry.name, (string[])entry.children);

                        cellNameToCellBlock.Add(entry.name, cel);
                    }
                }

                foreach (var entry in children)
                {
                    CellBlock start0 = null;
                    CellBlock[] targetList = entry.Value.Select(y => cellNameToCellBlock[y.Substring(0, y.LastIndexOf(":"))]).ToArray();
                    int[] portList = entry.Value.Select(y => Convert.ToInt32(y.Substring(y.LastIndexOf(":") + 1))).ToArray();

                    if (entry.Key == "$synth")
                    {
                    }
                    else
                    {
                        start0 = cellNameToCellBlock[entry.Key];
                    }

                    for(int i = 0; i < targetList.Length; i++)
                    {
                        CellBlock target = targetList[i];
                        int port = portList[i];

                        CellBlock start = start0;

                        if (start == null)
                        {
                            start = new CellBlock();
                            start.x = target.x;
                            start.y = target.y + 1;
                        }

                        bool horizontal = start.y == target.y;

                        if ((start.x - target.x) * (start.x - target.x) + (start.y - target.y) * (start.y - target.y) != 1)  // 隣接していなければエラー
                        {
                            throw new PatchFormatException();
                        }

                        foreach (var arr in arrows)
                        {
                            if (
                                arr.pos1x == start.x &&
                                arr.pos1y == start.y &&
                                arr.pos2x == target.x &&
                                arr.pos2y == target.y)
                            {
                                if (arr.direction != ArrowDirection.None) throw new PatchFormatException();

                                arr.direction = horizontal ?
                                    (port == 0 ? ArrowDirection.Left : ArrowDirection.LeftAlt) :
                                    (port == 0 ? ArrowDirection.Up : ArrowDirection.UpAlt);  // Altの実装な
                            }
                            else if (
                               arr.pos2x == start.x &&
                               arr.pos2y == start.y &&
                               arr.pos1x == target.x &&
                               arr.pos1y == target.y)
                            {
                                if (arr.direction != ArrowDirection.None) throw new PatchFormatException();

                                arr.direction = horizontal ?
                                    (port == 0 ? ArrowDirection.Right : ArrowDirection.RightAlt) :
                                    (port == 0 ? ArrowDirection.Down : ArrowDirection.DownAlt);  // Altの実装な
                            }
                        }
                    }
                }
            }
            catch (System.Xml.XmlException ex)
            {
                MessageBox.Show("パッチを読み込むことができませんでした：" + ex.ToString());
            }
        }
    }
}
