using Codeplex.Data;
using HatoDSP;
using HatoLib;
using HatoPlayer;
using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HatoSynthGUI
{
    /// <summary>
    /// 空の Windows Form に対してコントロールを作成し、イベントハンドラを結び付けます。
    /// </summary>
    public class SynthGUIHandler : IDisposable
    {
        //************************** 設定項目 **************************

        /// <summary>
        /// 画面左の、セルを配置する表の大きさ (個)
        /// </summary>
        readonly Size TableSize = new Size(6, 5);

        /// <summary>
        /// 画面左のセル1個の大きさ (pixel)
        /// </summary>
        readonly int CellSize = 64;

        /// <summary>
        /// 画面左のセルの周辺の余白の太さ (pixel)
        /// </summary>
        readonly int CellMargin = 8;

        //************************** 設定項目ここまで **************************

        Form form;
        SplitContainer splitContainer1;
        ContextMenuStrip contextMenuStrip2;
        BlockPresetLibrary library;
        AsioHandler asio;
        BlockTableManager btable;

        public enum ArrowDirection
        {
            None = 0,
            Up,
            Right,
            Down,
            Left,
            UpAlt,
            RightAlt,
            DownAlt,
            LeftAlt
        }

        /// <summary>
        /// それぞれのマスにどのセルが入っているか。
        /// 【注意】添字は y, x の順
        /// </summary>
        List<ArrowSummary> arrows;
        

        int dragx = 0, dragy = 0;  // ドラッグ開始時の e.X, e.Y の値
        bool dragging = false;
        PictureBox draggingBox = null;

        // セル1列分のx座標・y座標の差
        int CellTableInterval
        {
            get { return CellMargin * 2 + CellSize; }
        }

        /// <summary>
        /// 指定されたフォームにHatoSynthのGUIを作成し、イベントハンドラを登録します。
        /// </summary>
        public SynthGUIHandler(Form form)
        {
            this.form = form;

            arrows = new List<ArrowSummary>();

            library = new BlockPresetLibrary();

            btable = new BlockTableManager(TableSize);

            this.Load();
        }

        void finishDragging()
        {
            int posx = (draggingBox.Left + CellSize / 2) / CellTableInterval;
            int posy = (draggingBox.Top + CellSize / 2) / CellTableInterval;

            if (posx < 0) posx = 0;
            if (posx >= TableSize.Width) posx = TableSize.Width - 1;
            if (posy < 0) posy = 0;
            if (posy >= TableSize.Height) posy = TableSize.Height - 1;

            if (btable.TryMove(draggingBox, ref posx, ref posy))
            {
            }
            else
            {
            }

            draggingBox.Left = posx * CellTableInterval + CellMargin;
            draggingBox.Top = posy * CellTableInterval + CellMargin;
            draggingBox = null;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (dragging)
                {
                    finishDragging();
                }

                dragx = e.X;
                dragy = e.Y;
                draggingBox = (PictureBox)sender;
                splitContainer1.Panel1.Controls.SetChildIndex(draggingBox, 0);
                dragging = true;
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                draggingBox.Left += e.X - dragx;
                draggingBox.Top += e.Y - dragy;
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (dragging)
                {
                    finishDragging();
                }

                dragging = false;
            }
        }

        private void pictureBox2_DoubleClick(object sender, EventArgs e)
        {
            int x, y;

            if (btable.IsFull(out x, out y))
            {
                return;
            }

            BlockPresetLibrary.BlockPreset preset = null;

            string sendername = ((PictureBox)sender).Name;
            if (sendername.StartsWith("CellPreset_"))  // どのプリセット画像が選択されたか
            {
                int presetId = Int32.Parse(sendername.Substring(11));
                preset = library.Presets[presetId];
            }
            else
            {
                // マネージ コードのアサーション
                // https://msdn.microsoft.com/ja-jp/library/ttcc4x86(v=vs.110).aspx
                Debug.Assert(false, "何か不幸なことが起きた");

                preset = library.Presets[0];
            }

            var p = new PictureBox();
            //p.Image = (Image)((PictureBox)sender).Image.Clone();
            p.Image = ((PictureBox)sender).Image;
            p.Left = x * CellTableInterval + CellMargin;
            p.Top = y * CellTableInterval + CellMargin;
            p.Size = new System.Drawing.Size(CellSize, CellSize);
            p.SizeMode = PictureBoxSizeMode.Zoom;
            p.BorderStyle = BorderStyle.None;
            p.Cursor = Cursors.SizeAll;

            p.MouseDown += pictureBox1_MouseDown;
            p.MouseMove += pictureBox1_MouseMove;
            p.MouseUp += pictureBox1_MouseUp;

            p.ContextMenuStrip = contextMenuStrip2;

            btable.Add(p, x, y, preset);

            splitContainer1.Panel1.Controls.Add(p);
            splitContainer1.Panel1.Controls.SetChildIndex(p, 0);
        }

        private void arrowX_Click(object sender, EventArgs e)
        {
            var p = (PictureBox)sender;
            int arrowId = 0;  // XとYの通し

            if (p.Name.StartsWith("ArrowX_"))
            {
                arrowId = Int32.Parse(p.Name.Substring(7));
            }
            else
            {
                Debug.Assert(false, "arrowのハンドラが正しく結び付けられていません。");
                arrowId = 0;
            }

            if (p.ImageLocation == @"cells\arrow_00000.png" || p.ImageLocation == null)
            {
                p.ImageLocation = @"cells\arrow_00002.png";
                arrows[arrowId].direction = ArrowDirection.Right;
            }
            else if (p.ImageLocation == @"cells\arrow_00002.png")
            {
                p.ImageLocation = @"cells\arrow_00004.png";
                arrows[arrowId].direction = ArrowDirection.Left;
            }
            else if (p.ImageLocation == @"cells\arrow_00004.png")
            {
                p.ImageLocation = @"cells\arrow_00006.png";
                arrows[arrowId].direction = ArrowDirection.RightAlt;
            }
            else if (p.ImageLocation == @"cells\arrow_00006.png")
            {
                p.ImageLocation = @"cells\arrow_00008.png";
                arrows[arrowId].direction = ArrowDirection.LeftAlt;
            }
            else
            {
                p.ImageLocation = @"cells\arrow_00000.png";
                arrows[arrowId].direction = ArrowDirection.None;
            }
        }

        private void arrowY_Click(object sender, EventArgs e)
        {
            var p = (PictureBox)sender;
            int arrowId = 0;

            if (p.Name.StartsWith("ArrowY_"))
            {
                arrowId = Int32.Parse(p.Name.Substring(7));
            }
            else
            {
                Debug.Assert(false, "arrowのハンドラが正しく結び付けられていません。");
                arrowId = 0;
            }

            if (p.ImageLocation == @"cells\arrow_00000.png" || p.ImageLocation == null)
            {
                p.ImageLocation = @"cells\arrow_00003.png";
                arrows[arrowId].direction = ArrowDirection.Down;
            }
            else if (p.ImageLocation == @"cells\arrow_00003.png")
            {
                p.ImageLocation = @"cells\arrow_00001.png";
                arrows[arrowId].direction = ArrowDirection.Up;
            }
            else if (p.ImageLocation == @"cells\arrow_00001.png")
            {
                p.ImageLocation = @"cells\arrow_00007.png";
                arrows[arrowId].direction = ArrowDirection.DownAlt;
            }
            else if (p.ImageLocation == @"cells\arrow_00007.png")
            {
                p.ImageLocation = @"cells\arrow_00005.png";
                arrows[arrowId].direction = ArrowDirection.UpAlt;
            }
            else
            {
                p.ImageLocation = @"cells\arrow_00000.png";
                arrows[arrowId].direction = ArrowDirection.None;
            }
        }

        private void Load()
        {
            {
                contextMenuStrip2 = new ContextMenuStrip();

                ToolStripMenuItem item1 = new ToolStripMenuItem() { Text = "削除(&D)" };
                item1.Click += DeleteToolStripMenuItem_Click;

                contextMenuStrip2.Items.AddRange(new ToolStripItem[] {
                    item1
                });
            }

            {
                SplitContainer spc = new SplitContainer();
                spc.Name = "splitContainer1";
                spc.BorderStyle = BorderStyle.Fixed3D;
                spc.Dock = DockStyle.Fill;
                spc.Orientation = Orientation.Vertical;
                spc.SplitterDistance = spc.Size.Width * 3 / 4;

                form.Controls.Add(spc);

                splitContainer1 = spc;
            }

            //form.KeyDown += form_KeyDown;
            //splitContainer1.KeyDown += form_KeyDown;
            splitContainer1.KeyDown += form_KeyDown;
            //splitContainer1.Panel1.KeyDown += form_KeyDown;

            // 画像ファイルを高速に読み込むには？［2.0のみ、C#、VB］
            // http://www.atmarkit.co.jp/fdotnet/dotnettips/597fastloadimg/fastloadimg.html

            // ←読み込みが驚くほど速くなった
            // （と思ったけれど ImageLocation を使ってたのが遅かったっぽい）

            {
                var size = CellMargin * 2;

                int arrowId;
                int arrowId2 = 0;

                for (arrowId = 0; arrowId < (TableSize.Width - 1) * TableSize.Height; arrowId++, arrowId2++)
                {
                    int x1 = arrowId % (TableSize.Width - 1);
                    int y1 = arrowId / (TableSize.Width - 1);

                    // 水平方向（左右向き）の矢印
                    var p = new PictureBox();
                    //p.Image = Image.FromFile(@"cells\arrow_00000.png");
                    //p.ImageLocation = @"cells\arrow_00000.png";
                    p.Image = Image.FromStream(File.OpenRead(@"cells\arrow_00000.png"), false, false);
                    p.Name = "ArrowX_" + arrowId2;
                    p.Left = (x1 + 1) * CellTableInterval - CellMargin;
                    p.Top = y1 * CellTableInterval + CellTableInterval / 2 - CellMargin;
                    p.Size = new System.Drawing.Size(size, size);
                    p.SizeMode = PictureBoxSizeMode.Zoom;
                    p.BorderStyle = BorderStyle.None;
                    p.Cursor = Cursors.Hand;

                    p.MouseDown += arrowX_Click;

                    splitContainer1.Panel1.Controls.Add(p);

                    arrows.Add(new ArrowSummary(x1, y1, x1 + 1, y1));
                }

                Debug.Assert(arrowId2 == (TableSize.Width - 1) * TableSize.Height);

                for (arrowId = 0; arrowId < TableSize.Width * TableSize.Height; arrowId++, arrowId2++)
                {
                    int x1 = arrowId % TableSize.Width;
                    int y1 = arrowId / TableSize.Width;

                    // 垂直方向（左右向き）の矢印
                    var p = new PictureBox();
                    //p.Image = Image.FromFile(@"cells\arrow_00000.png");
                    //p.ImageLocation = @"cells\arrow_00000.png";
                    p.Image = Image.FromStream(File.OpenRead(@"cells\arrow_00000.png"), false, false);
                    p.Name = "ArrowY_" + arrowId2;
                    p.Left = x1 * CellTableInterval + CellTableInterval / 2 - CellMargin;
                    p.Top = (y1 + 1) * CellTableInterval - CellMargin;
                    p.Size = new System.Drawing.Size(size, size);
                    p.SizeMode = PictureBoxSizeMode.Zoom;
                    p.BorderStyle = BorderStyle.None;
                    p.Cursor = Cursors.Hand;

                    p.MouseDown += arrowY_Click;

                    splitContainer1.Panel1.Controls.Add(p);

                    arrows.Add(new ArrowSummary(x1, y1, x1, y1 + 1));
                }
            }

            // 画面左のセル置き場
            for (int cellId = 0; cellId < TableSize.Width * TableSize.Height; cellId++)
            {
                var p = new PictureBox();
                //p.Image = Image.FromFile(@"cells\cell_00000.png");
                //p.ImageLocation = @"cells\cell_00000.png";
                p.Image = Image.FromStream(File.OpenRead(@"cells\cell_00000.png"), false, false);
                p.Left = cellId % TableSize.Width * CellTableInterval + CellMargin;
                p.Top = cellId / TableSize.Width * CellTableInterval + CellMargin;
                p.Size = new System.Drawing.Size(CellSize, CellSize);
                p.SizeMode = PictureBoxSizeMode.Zoom;
                p.BorderStyle = BorderStyle.None;

                splitContainer1.Panel1.Controls.Add(p);
            }

            // 画面右のセル一覧
            for (int cellId = 0; cellId < library.Presets.Count; cellId++)
            {
                var p = new PictureBox();
                //p.Image = Image.FromFile(@"cells\cell_0000" + (cellId + 1) + ".png");
                //p.ImageLocation = @"cells\cell_0000" + (cellId + 1) + ".png";
                p.Image = Image.FromStream(File.OpenRead(HatoPath.FromAppDir(@"cells\cell_000" +
                    library.Presets[cellId].GraphicId / 10 + library.Presets[cellId].GraphicId % 10 + ".png")), false, false);
                p.Name = "CellPreset_" + cellId;
                p.Left = cellId % 2 * 40 + 4;
                p.Top = cellId / 2 * 40 + 4;
                p.Size = new System.Drawing.Size(32, 32);
                p.SizeMode = PictureBoxSizeMode.Zoom;
                p.BorderStyle = BorderStyle.None;
                p.Cursor = Cursors.Hand;

                p.DoubleClick += pictureBox2_DoubleClick;

                splitContainer1.Panel2.Controls.Add(p);
            }
        }

        class AAAA
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

        private class ArrowSummary
        {
            public int pos1x;  // 矢印の左または上
            public int pos1y;
            public int pos2x;  // 矢印の右または下
            public int pos2y;
            public ArrowDirection direction;

            public ArrowSummary(int x1, int y1, int x2, int y2)
            {
                Debug.Assert(x1 <= x2 && y1 <= y2);  // GUI関連は要求レベルが高くて、バグが入り込みやすそうで困る

                pos1x = x1;
                pos1y = y1;
                pos2x = x2;
                pos2y = y2;
                direction = ArrowDirection.None;
            }
        }

        void form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.P && e.Control)
            {
                AAAA[,] cells = new AAAA[TableSize.Height, TableSize.Width];
                for (int y = 0; y < TableSize.Height; y++)
                {
                    for (int x = 0; x < TableSize.Width; x++)
                    {
                        BlockPresetLibrary.BlockPreset preset;

                        if (btable.TryGetPreset(x, y, out preset))
                        {
                            // DynamicJsonを編集しようかと思ったけど難しすぎる・・・
                            // DynamicJsonを入れ子にできれば捗るのだけれど、
                            // そもそも遅延評価になっていないという罠。

                            AAAA cell = new AAAA
                            {
                                name = preset.DefaultName,  // FIXME: デフォルト名からユニークな名前に変更
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

                // FIXME: ↓ Don't Repeat Yourself!!!!!!!!!!!!!

                // 最下段以外の矢印
                for (int arrowId = 0; arrowId < arrows.Count; arrowId++)
                {
                    AAAA src = null, dst = null;
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

                AAAA start = null;

                // 最下段の矢印について
                for (int arrowId = 0; arrowId < arrows.Count; arrowId++)
                {
                    AAAA src = null, dst = null;
                    int port = 0;
                    ArrowSummary sm = arrows[arrowId];

                    if (sm.pos2y != TableSize.Height) continue;  // 最下段の矢印ではなかった場合

                    if ((sm.direction == ArrowDirection.Down || sm.direction == ArrowDirection.DownAlt)
                        && cells[sm.pos1y, sm.pos1x] != null)
                    {
                        //start = cells[y, x];  // [y,x] がスタート地点（複数あるかも）
                        start = new AAAA
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

                    RunAsio(str);  // ← 100msくらい
                    // RunAsio から返ってきてから、callback関数が最初に呼ばれるまで 330ms くらい掛かっている？
                    // ASIOの制約かもしれないですね。
                    // それともアセンブリ(.dllファイル)の読み込みに時間が掛かっている・・・？
                }
            }
        }

        private InputDevice midiInDev;

        #region Midi入力のイベントハンドラ
        void midiInDev_ChannelMessageReceived(object sender, ChannelMessageEventArgs ev)
        {
            ChannelCommand cmd = ev.Message.Command;
            int n = ev.Message.Data1;  // ノート番号
            int vel = ev.Message.Data2;  // ベロシティ（ノートオン時のみ）

            switch (cmd)
            {
                case ChannelCommand.NoteOn:
                    synth.NoteOn(n);
                    Console.WriteLine("on  " + n);
                    break;
                case ChannelCommand.NoteOff:
                    synth.NoteOff(n);
                    Console.WriteLine("off " + n);
                    break;
            }
        }
        #endregion

        HatoSynthDevice synth = null;

        void RunAsio(string patch)
        {
            synth = new HatoSynthDevice(patch);
            synth.NoteOn(60);
            //synth.NoteOn(64);
            //synth.NoteOn(67);

            if (asio == null)
            {
                asio = new AsioHandler();

                // 再生が停止するまで AsioHandler を解放しないように・・・
                asio.Run(aBuf =>
                {
                    var buf2 = synth.Take(aBuf.SampleCount).Select(x => x.ToArray()).ToArray();  // 同じスレッドで処理しちゃったてへっ

                    for (int ch = 0; ch < aBuf.ChannelCount; ch++)
                    {
                        for (int i = 0; i < aBuf.SampleCount; i++)
                        {
                            aBuf.Buffer[ch][i] = buf2[ch][i];
                        }
                    }
                });

                // Midi入力デバイスの列挙
                for (int i = 0; i < InputDevice.DeviceCount; i++)
                {
                    var dev = InputDevice.GetDeviceCapabilities(i);

                    Console.WriteLine("in " + i + " : " + dev.name);
                }

                // midi入力の初期化
                if (InputDevice.DeviceCount >= 9)
                {
                    // FIXME: inputデバイスの選択
                    midiInDev = new InputDevice(8);  // Windowsからmidiデバイスを開く
                    midiInDev.ChannelMessageReceived += midiInDev_ChannelMessageReceived;  // コールバック関数の指定
                    midiInDev.StartRecording();  // 入力待機の開始
                }
            }
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PictureBox source = (PictureBox)contextMenuStrip2.SourceControl;  // メモ：うまく取得できないことがあるらしい
            if (source != null)
            {
                btable.Remove(source);

                splitContainer1.Panel1.Controls.Remove(source);
            }
        }

        #region implementation of IDisposable
        // Flag: Has Dispose already been called?
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            
            System.Diagnostics.Debug.Assert(disposing, "激おこ @ " + this.GetType().ToString());

            if (disposing)
            {
                // Free any other managed objects here.
                if (asio != null)
                {
                    asio.Dispose();
                }

                if (midiInDev != null)
                {
                    midiInDev.Dispose();
                }
            }

            // Free any unmanaged objects here.

            disposed = true;
        }
        
        ~SynthGUIHandler()
        {
            Dispose(false);
        }
        #endregion
    }
}
