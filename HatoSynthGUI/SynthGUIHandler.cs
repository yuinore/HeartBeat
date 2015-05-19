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

        /// <summary>
        /// 画面右の、セルのカタログを配置する表の横の長さ (個)
        /// </summary>
        readonly int CatalogWidth = 4;

        //************************** 設定項目ここまで **************************

        Form form;
        SplitContainer splitContainer1;
        TabControl tabControl1;
        ScrollableControl CellMatrixContainer;
        ScrollableControl CellCatalogContainer;
        ScrollableControl CellDetailContainer;

        ContextMenuStrip contextMenuStrip2;
        BlockPresetLibrary library;
        AsioHandler asio;
        BlockTableManager btable;

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
                CellMatrixContainer.Controls.SetChildIndex(draggingBox, 0);
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

        PictureBox currentlyDetailOpenedPictureBox = null;

        private void cellParamsTextBox_TextChanged(object sender, EventArgs e)
        {
            BlockPatch preset;

            btable.TryGetBlockPatch(currentlyDetailOpenedPictureBox, out preset);

            int paramIdx = Convert.ToInt32(((TextBox)sender).Name);

            float val;
            if (Single.TryParse(((TextBox)sender).Text, out val))
            {
                preset.Ctrl[paramIdx] = val;
            }
        }

        private void ClearDetailContainer()
        {
            while (CellDetailContainer.Controls.Count >= 1)
            {
                CellDetailContainer.Controls.RemoveAt(CellDetailContainer.Controls.Count - 1);  // 逆順の方が速いかな？（未検証）
            }
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            PictureBox pBox = sender as PictureBox;
            BlockPatch preset;

            Debug.Assert(pBox != null, "あれれ～おかしいぞ～");

            currentlyDetailOpenedPictureBox = pBox;

            tabControl1.SelectedIndex = 1;
            
            ClearDetailContainer();

            btable.TryGetBlockPatch(pBox, out preset);
            
            CellDetailContainer.Controls.Add(new Label() { Text = preset.Name });
            CellDetailContainer.Controls.Add(new Label() { Text = "" });

            CellParameter[] paramsList = (new CellTree(preset.Name, preset.ModuleName)).Generate().ParamsList;  // !?!?!?!?

            for (int i = 0; i < paramsList.Length; i++)
            {
                CellParameter p = paramsList[i];

                CellDetailContainer.Controls.Add(new Label() { Text = p.Name });

                TextBox tbox = new TextBox() { Text = preset.Ctrl[i].ToString() };
                tbox.Name = "" + i;
                tbox.TextChanged += cellParamsTextBox_TextChanged;
                CellDetailContainer.Controls.Add(tbox);
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
            p.DoubleClick += pictureBox1_DoubleClick;

            p.ContextMenuStrip = contextMenuStrip2;

            btable.Add(p, x, y, preset);

            CellMatrixContainer.Controls.Add(p);
            CellMatrixContainer.Controls.SetChildIndex(p, 0);
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
            //******** 設定 ********
            form.KeyPreview = true;

            {
                //******** 右クリックメニュー ********
                contextMenuStrip2 = new ContextMenuStrip();

                ToolStripMenuItem item1 = new ToolStripMenuItem() { Text = "削除(&D)" };
                item1.Click += DeleteToolStripMenuItem_Click;

                contextMenuStrip2.Items.AddRange(new ToolStripItem[] {
                    item1
                });
            }

            {
                //******** メニューバーを配置するための枠 ********
                ToolStripContainer tsc = new ToolStripContainer();  // ToolStripPanelでも可！！！！
                tsc.Dock = DockStyle.Fill;

                {
                    //******** メニューバー ********
                    MenuStrip ms = new MenuStrip();

                    ToolStripMenuItem item1 = new ToolStripMenuItem() { Text = "ファイル(&F)" };
                    {
                        ToolStripMenuItem item1_1 = new ToolStripMenuItem() { Text = "名前を付けて保存(&S)" };
                        item1_1.Click += saveAsPatchToolStripMenuItem_Click;
                        item1.DropDownItems.Add(item1_1);

                        ToolStripMenuItem item1_2 = new ToolStripMenuItem() { Text = "クイックセーブ" };
                        item1_2.Click += savePatchToolStripMenuItem_Click;
                        item1.DropDownItems.Add(item1_2);
                    }
                    ms.Items.Add(item1);

                    tsc.TopToolStripPanel.Controls.Add(ms);
                }
                form.Controls.Add(tsc);

                //******** 左右に２分割するコンテナ ********
                SplitContainer spc = new SplitContainer();
                spc.Name = "splitContainer1";
                spc.BorderStyle = BorderStyle.Fixed3D;
                spc.Dock = DockStyle.Fill;
                spc.Orientation = Orientation.Vertical;
                spc.SplitterDistance = spc.Size.Width * 3 / 4;

                tsc.ContentPanel.Controls.Add(spc);

                splitContainer1 = spc;

                {
                    TabControl tab = new TabControl();
                    tab.Dock = DockStyle.Fill;

                    TabPage tpage1 = new TabPage();
                    tpage1.Name = "tab1";
                    tpage1.Text = "Catalog";
                    tpage1.Padding = new System.Windows.Forms.Padding(3);
                    tpage1.UseVisualStyleBackColor = true;
                    tab.Controls.Add(tpage1);

                    TabPage tpage2 = new TabPage();
                    tpage2.Name = "tab2";
                    tpage2.Text = "Detail";
                    tpage2.Padding = new System.Windows.Forms.Padding(3);
                    tpage2.UseVisualStyleBackColor = true;
                    tab.Controls.Add(tpage2);

                    FlowLayoutPanel flow = new FlowLayoutPanel();
                    flow.Dock = DockStyle.Fill;
                    flow.FlowDirection = FlowDirection.TopDown;
                    tpage2.Controls.Add(flow);

                    splitContainer1.Panel2.Controls.Add(tab);

                    CellMatrixContainer = splitContainer1.Panel1;
                    CellCatalogContainer = tpage1;
                    CellDetailContainer = flow;
                    tabControl1 = tab;
                }
            }

            form.KeyDown += form_KeyDown;
            //splitContainer1.KeyDown += form_KeyDown;
            //splitContainer1.Panel1.KeyDown += form_KeyDown;
            //splitContainer1.Panel2.KeyDown += form_KeyDown;

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

                    CellMatrixContainer.Controls.Add(p);

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

                    CellMatrixContainer.Controls.Add(p);

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

                CellMatrixContainer.Controls.Add(p);
            }

            // 画面右のセル一覧
            for (int cellId = 0; cellId < library.Presets.Count; cellId++)
            {
                var p = new PictureBox();
                //p.Image = Image.FromFile(@"cells\cell_0000" + (cellId + 1) + ".png");
                //p.ImageLocation = @"cells\cell_0000" + (cellId + 1) + ".png";
                p.Image = Image.FromStream(File.OpenRead(HatoPath.FromAppDir(@"cells\cell_" +
                    String.Format("{0:00000}", library.Presets[cellId].GraphicId) + ".png")), false, false);
                p.Name = "CellPreset_" + cellId;
                p.Left = cellId % CatalogWidth * 40 + 4;
                p.Top = cellId / CatalogWidth * 40 + 4;
                p.Size = new System.Drawing.Size(32, 32);
                p.SizeMode = PictureBoxSizeMode.Zoom;
                p.BorderStyle = BorderStyle.None;
                p.Cursor = Cursors.Hand;

                p.DoubleClick += pictureBox2_DoubleClick;

                CellCatalogContainer.Controls.Add(p);
            }
        }

        /// <summary>
        /// ファイル → 名前を付けて保存
        /// </summary>
        private void saveAsPatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string patch = PatchIO.Serialize(TableSize, btable, arrows);

            if (patch != null)
            {
                // 「名前を付けて保存」ダイアログボックスを表示する
                // http://dobon.net/vb/dotnet/form/savefiledialog.html

                string filename;
                {
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Title = "Save Patch File As";
                    sfd.FileName = "patch 1.hatp";
                    sfd.Filter = "HatoSynth Patch (*.hatp)|*.hatp";
                    sfd.FilterIndex = 0;
                    sfd.InitialDirectory = HatoPath.FromAppDir("");  // TODO: カレントディレクトリを記憶するようにする。

                    //ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
                    sfd.RestoreDirectory = true;

                    //ダイアログを表示する
                    if (sfd.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                    filename = sfd.FileName;
                }

                try
                {
                    File.WriteAllText(filename, patch);

                    MessageBox.Show("保存が完了しました。", "保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("保存に失敗しました：" + ex.ToString(), "保存", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("保存するパッチがありません。", "保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// ファイル → クイックセーブ
        /// </summary>
        private void savePatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string patch = PatchIO.Serialize(TableSize, btable, arrows);

            if (patch != null)
            {
                try
                {
                    File.WriteAllText(HatoPath.FromAppDir("quicksave.hatp"), patch);

                    MessageBox.Show("保存が完了しました。", "保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("保存に失敗しました：" + ex.ToString(), "保存", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        void form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.P && e.Control)
            {
                string patch = PatchIO.Serialize(TableSize, btable, arrows);

                if (patch != null)
                {
                    RunAsio(patch);  // ← 100msくらい
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
            int bend = (n | (vel << 7)) - 8192;  // ピッチベンド(-8192～8191)

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
                case ChannelCommand.PitchWheel:
                    synth.PitchBend(bend);
                    Console.WriteLine("bend " + bend);
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
                btable.Remove(source);  // テーブル管理から削除

                CellMatrixContainer.Controls.Remove(source);  // pictureBoxを削除

                if (currentlyDetailOpenedPictureBox == source)
                {
                    ClearDetailContainer();
                }
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
