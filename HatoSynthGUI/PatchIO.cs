using Codeplex.Data;
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
            public float[] ctrl { get; set; }
            public string[] children { get; set; }
            public int pos { get; set; }

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

            public Tuple<int, int> GetPos()
            {
                int x = (
                    ((pos & 0x000007) >> 0) |
                    ((pos & 0x0001C0) >> 3) |
                    ((pos & 0x007000) >> 6) |
                    ((pos & 0x1C0000) >> 9));

                int y = (
                    ((pos & 0x000038) >> 3) |
                    ((pos & 0x000D00) >> 6) |
                    ((pos & 0x038000) >> 9) |
                    ((pos & 0xD00000) >> 12));

                return new Tuple<int, int>(x, y);
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
                            children = new string[0] { }
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
    }
}
