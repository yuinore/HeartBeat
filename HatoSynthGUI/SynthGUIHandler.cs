using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HatoSynthGUI
{
    /// <summary>
    /// 空の Windows Form に対してコントロールを作成し、イベントハンドラを結び付けます。
    /// </summary>
    public class SynthGUIHandler
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

        /// <summary>
        /// PictureBox と、その位置の組
        /// </summary>
        private class CellBlock
        {
            public int uniqueId;
            public PictureBox pBox;
            public int y;
            public int x;
        }

        /// <summary>
        /// それぞれのマスにどのセルが入っているか。
        /// 【注意】添字は y, x の順
        /// </summary>
        CellBlock[,] table;
        
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

            cb.uniqueId = 0;
            cb.pBox = p;
            cb.x = x;
            cb.y = y;

            splitContainer1.Panel1.Controls.Add(p);
            splitContainer1.Panel1.Controls.SetChildIndex(p, 0);
        }

        private void arrowX_Click(object sender, EventArgs e)
        {
            var p = (PictureBox)sender;
            if (p.ImageLocation == @"cells\arrow_00000.png")
            {
                p.ImageLocation = @"cells\arrow_00002.png";
            }
            else if (p.ImageLocation == @"cells\arrow_00002.png")
            {
                p.ImageLocation = @"cells\arrow_00004.png";
            }
            else if (p.ImageLocation == @"cells\arrow_00004.png")
            {
                p.ImageLocation = @"cells\arrow_00006.png";
            }
            else if (p.ImageLocation == @"cells\arrow_00006.png")
            {
                p.ImageLocation = @"cells\arrow_00008.png";
            }
            else
            {
                p.ImageLocation = @"cells\arrow_00000.png";
            }
        }

        private void arrowY_Click(object sender, EventArgs e)
        {
            var p = (PictureBox)sender;
            if (p.ImageLocation == @"cells\arrow_00000.png")
            {
                p.ImageLocation = @"cells\arrow_00001.png";
            }
            else if (p.ImageLocation == @"cells\arrow_00001.png")
            {
                p.ImageLocation = @"cells\arrow_00003.png";
            }
            else if (p.ImageLocation == @"cells\arrow_00003.png")
            {
                p.ImageLocation = @"cells\arrow_00005.png";
            }
            else if (p.ImageLocation == @"cells\arrow_00005.png")
            {
                p.ImageLocation = @"cells\arrow_00007.png";
            }
            else
            {
                p.ImageLocation = @"cells\arrow_00000.png";
            }
        }

        private void Load()
        {
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

            {
                var size = CellMargin * 2;

                for (int arrowId = 0; arrowId < (TableSize.Width - 1) * TableSize.Height; arrowId++)
                {
                    // 水平方向（左右向き）の矢印
                    var p = new PictureBox();
                    p.ImageLocation = @"cells\arrow_00000.png";
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
                    p.ImageLocation = @"cells\arrow_00000.png";
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
                p.ImageLocation = @"cells\cell_00000.png";
                p.Left = cellId % TableSize.Width * CellTableInterval + CellMargin;
                p.Top = cellId / TableSize.Width * CellTableInterval + CellMargin;
                p.Size = new System.Drawing.Size(CellSize, CellSize);
                p.SizeMode = PictureBoxSizeMode.Zoom;
                p.BorderStyle = BorderStyle.None;

                splitContainer1.Panel1.Controls.Add(p);
            }

            // 画面右のセル一覧
            for (int cellId = 0; cellId < 9; cellId++)
            {
                var p = new PictureBox();
                //p.Image = Image.FromFile(@"cells\cell_0000" + cellId + ".png");
                p.ImageLocation = @"cells\cell_0000" + (cellId + 1) + ".png";
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
    }
}
