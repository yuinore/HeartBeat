using Codeplex.Data;
using HatoDSP;
using HatoPlayer;
using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// PictureBox と、その位置の組
        /// </summary>
        private class CellBlock
        {
            public PictureBox pBox;
            public BlockPresetLibrary.BlockPreset preset;
            public int y;
            public int x;
        }

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
        CellBlock[,] table;
        ArrowDirection[,] arrowX, arrowY;
        
        /// <summary>
        /// tableをPictureBoxで逆引きします。
        /// </summary>
        CellBlock pictureboxToCellblock(PictureBox p)
        {
            foreach (var cb in table)
            {
                if (cb == null) continue;

                if (cb.pBox == p)
                {
                    return cb;  // TODO: 計算量削減のためのDictionary作成
                }
            }

            throw new KeyNotFoundException();
        }

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

            table = new CellBlock[TableSize.Height, TableSize.Width];
            arrowX = new ArrowDirection[TableSize.Height, TableSize.Width - 1];
            arrowY = new ArrowDirection[TableSize.Height, TableSize.Width];
            library = new BlockPresetLibrary();

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

            CellBlock cb = null;

            if (table[posy, posx] == null)
            {
                cb = pictureboxToCellblock(draggingBox);

                table[cb.y, cb.x] = null;

                cb.x = posx;
                cb.y = posy;
                table[posy, posx] = cb;
            }
            else
            {
                cb = pictureboxToCellblock(draggingBox);
            }

            draggingBox.Left = cb.x * CellTableInterval + CellMargin;
            draggingBox.Top = cb.y * CellTableInterval + CellMargin;
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
            var cb = new CellBlock();
            int x = 0, y = 0;
            for (y = 0; y < TableSize.Height; y++)
            {
                for (x = 0; x < TableSize.Width; x++)
                {
                    if (table[y, x] == null)
                    {
                        table[y, x] = cb;

                        // gotoの濫用に注意
                        goto break1;  // C# に goto 文ってあったの！？！？
                    }
                }
            }

            return;  // もしテーブルに空きがなかったら何もしない

            break1:

            string sendername = ((PictureBox)sender).Name;
            if (sendername.StartsWith("CellPreset_"))
            {
                int presetId = Int32.Parse(sendername.Substring(11));
                cb.preset = library.Presets[presetId];
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

            cb.pBox = p;
            cb.x = x;
            cb.y = y;

            splitContainer1.Panel1.Controls.Add(p);
            splitContainer1.Panel1.Controls.SetChildIndex(p, 0);
        }

        private void arrowX_Click(object sender, EventArgs e)
        {
            var p = (PictureBox)sender;
            int y = 0, x = 0;
            if (p.Name.StartsWith("ArrowX_"))
            {
                int arrowId = Int32.Parse(p.Name.Substring(7));
                x = arrowId % (TableSize.Width - 1);
                y = arrowId / (TableSize.Width - 1);
            }
            if (p.ImageLocation == @"cells\arrow_00000.png" || p.ImageLocation == null)
            {
                p.ImageLocation = @"cells\arrow_00002.png";
                arrowX[y, x] = ArrowDirection.Right;
            }
            else if (p.ImageLocation == @"cells\arrow_00002.png")
            {
                p.ImageLocation = @"cells\arrow_00004.png";
                arrowX[y, x] = ArrowDirection.Left;
            }
            else if (p.ImageLocation == @"cells\arrow_00004.png")
            {
                p.ImageLocation = @"cells\arrow_00006.png";
                arrowX[y, x] = ArrowDirection.RightAlt;
            }
            else if (p.ImageLocation == @"cells\arrow_00006.png")
            {
                p.ImageLocation = @"cells\arrow_00008.png";
                arrowX[y, x] = ArrowDirection.LeftAlt;
            }
            else
            {
                p.ImageLocation = @"cells\arrow_00000.png";
                arrowX[y, x] = ArrowDirection.None;
            }
        }

        private void arrowY_Click(object sender, EventArgs e)
        {
            var p = (PictureBox)sender;
            int y = 0, x = 0;
            if (p.Name.StartsWith("ArrowY_"))
            {
                int arrowId = Int32.Parse(p.Name.Substring(7));
                x = arrowId % TableSize.Width;
                y = arrowId / TableSize.Width;
            }
            if (p.ImageLocation == @"cells\arrow_00000.png" || p.ImageLocation == null)
            {
                p.ImageLocation = @"cells\arrow_00003.png";
                arrowY[y, x] = ArrowDirection.Down;
            }
            else if (p.ImageLocation == @"cells\arrow_00003.png")
            {
                p.ImageLocation = @"cells\arrow_00001.png";
                arrowY[y, x] = ArrowDirection.Up;
            }
            else if (p.ImageLocation == @"cells\arrow_00001.png")
            {
                p.ImageLocation = @"cells\arrow_00007.png";
                arrowY[y, x] = ArrowDirection.DownAlt;
            }
            else if (p.ImageLocation == @"cells\arrow_00007.png")
            {
                p.ImageLocation = @"cells\arrow_00005.png";
                arrowY[y, x] = ArrowDirection.UpAlt;
            }
            else
            {
                p.ImageLocation = @"cells\arrow_00000.png";
                arrowY[y, x] = ArrowDirection.None;
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

                for (int arrowId = 0; arrowId < (TableSize.Width - 1) * TableSize.Height; arrowId++)
                {
                    // 水平方向（左右向き）の矢印
                    var p = new PictureBox();
                    //p.Image = Image.FromFile(@"cells\arrow_00000.png");
                    //p.ImageLocation = @"cells\arrow_00000.png";
                    p.Image = Image.FromStream(File.OpenRead(@"cells\arrow_00000.png"), false, false);
                    p.Name = "ArrowX_" + arrowId;
                    p.Left = (arrowId % (TableSize.Width - 1) + 1) * CellTableInterval - CellMargin;
                    p.Top = arrowId / (TableSize.Width - 1) * CellTableInterval + CellTableInterval / 2 - CellMargin;
                    p.Size = new System.Drawing.Size(size, size);
                    p.SizeMode = PictureBoxSizeMode.Zoom;
                    p.BorderStyle = BorderStyle.None;
                    p.Cursor = Cursors.Hand;

                    p.MouseDown += arrowX_Click;

                    splitContainer1.Panel1.Controls.Add(p);
                }

                for (int arrowId = 0; arrowId < TableSize.Width * TableSize.Height; arrowId++)
                {
                    // 垂直方向（左右向き）の矢印
                    var p = new PictureBox();
                    //p.Image = Image.FromFile(@"cells\arrow_00000.png");
                    //p.ImageLocation = @"cells\arrow_00000.png";
                    p.Image = Image.FromStream(File.OpenRead(@"cells\arrow_00000.png"), false, false);
                    p.Name = "ArrowY_" + arrowId;
                    p.Left = arrowId % TableSize.Width * CellTableInterval + CellTableInterval / 2 - CellMargin;
                    p.Top = (arrowId / TableSize.Width + 1) * CellTableInterval - CellMargin;
                    p.Size = new System.Drawing.Size(size, size);
                    p.SizeMode = PictureBoxSizeMode.Zoom;
                    p.BorderStyle = BorderStyle.None;
                    p.Cursor = Cursors.Hand;

                    p.MouseDown += arrowY_Click;

                    splitContainer1.Panel1.Controls.Add(p);
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
                p.Image = Image.FromStream(File.OpenRead(@"cells\cell_0000" + library.Presets[cellId].GraphicId + ".png"), false, false);
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
                        if (table[y, x] != null)
                        {
                            var preset = table[y, x].preset;


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

                            cells[y, x] = cell;
                        }
                    }
                }

                // FIXME: ↓ Don't Repeat Yourself!!!!!!!!!!!!!

                // 横向きの矢印について
                for (int y = 0; y < TableSize.Height; y++)
                {
                    for (int x = 0; x < TableSize.Width - 1; x++)
                    {
                        // [y,x] と [y,x+1] の間を結ぶ
                        AAAA src = null, dst = null;
                        int port = 0;

                        switch (arrowX[y, x])
                        {
                            case ArrowDirection.Right:
                                src = cells[y, x];
                                dst = cells[y, x + 1];
                                port = 0;
                                break;
                            case ArrowDirection.Left:
                                src = cells[y, x + 1];
                                dst = cells[y, x];
                                port = 0;
                                break;
                            case ArrowDirection.RightAlt:
                                src = cells[y, x];
                                dst = cells[y, x + 1];
                                port = 1;
                                break;
                            case ArrowDirection.LeftAlt:
                                src = cells[y, x + 1];
                                dst = cells[y, x];
                                port = 1;
                                break;
                        }

                        if (src != null)
                        {
                            //dst.AssignChildren(new CellTree[] { src });  // TODO: 複数指定
                            // ******** TODO
                            /////////////////////////////////////////////////////
                            IEnumerable<string> arr = dst.children;
                            arr = arr.Concat(new string[] { src.name + ":" + port });
                            dst.children = arr.ToArray();
                        }
                    }
                }

                // 縦向きの矢印について
                for (int y = 0; y < TableSize.Height - 1; y++)
                {
                    for (int x = 0; x < TableSize.Width; x++)
                    {
                        // [y,x] と [y+1,x] の間を結ぶ
                        AAAA src = null, dst = null;
                        int port = 0;

                        switch (arrowY[y, x])
                        {
                            case ArrowDirection.Down:
                                src = cells[y, x];
                                dst = cells[y + 1, x];
                                port = 0;
                                break;
                            case ArrowDirection.Up:
                                src = cells[y + 1, x];
                                dst = cells[y, x];
                                port = 0;
                                break;
                            case ArrowDirection.DownAlt:
                                src = cells[y, x];
                                dst = cells[y + 1, x];
                                port = 1;
                                break;
                            case ArrowDirection.UpAlt:
                                src = cells[y + 1, x];
                                dst = cells[y, x];
                                port = 1;
                                break;
                        }

                        if (src != null && dst != null)
                        {
                            //dst.AssignChildren(new CellTree[] { src });  // TODO: 複数指定
                            // ******** TODO
                            /////////////////////////////////////////////////////
                            IEnumerable<string> arr = dst.children;
                            arr = arr.Concat(new string[] { src.name + ":" + port });
                            dst.children = arr.ToArray();
                        }
                    }
                }

                AAAA start = null;

                {
                    int y = TableSize.Height - 1;  // 一番下の行

                    for (int x = 0; x < TableSize.Width; x++)
                    {
                        if ((arrowY[y, x] == ArrowDirection.Down || arrowY[y, x] == ArrowDirection.DownAlt) && cells[y, x] != null)
                        {
                            //start = cells[y, x];  // [y,x] がスタート地点（複数あるかも）
                            start = new AAAA
                            {
                                name = "$synth",
                                module = "",
                                children = new string[] { cells[y, x].name + ":0" },
                                ctrl = new float[0] { }
                            };
                        }
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

                    string str = json.ToString();
                    //string str = DynamicJson.Serialize(json);

                    // jsonを整形
                    dynamic parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(str);
                    str = Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
                    
                    RunAsio(str);
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
            PictureBox source = (PictureBox)contextMenuStrip2.SourceControl;
            if (source != null)
            {
                CellBlock cb = pictureboxToCellblock(source);
                table[cb.y, cb.x] = null;

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
