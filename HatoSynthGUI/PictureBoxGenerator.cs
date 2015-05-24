using HatoLib;
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
    class PictureBoxGenerator
    {
        readonly Size TableSize;
        readonly int CellSize;
        readonly int CellMargin;
        readonly int CatalogWidth;

        int CellTableInterval
        {
            get { return CellMargin * 2 + CellSize; }
        }

        public PictureBoxGenerator(Size tableSize, int cellSize, int cellMargin, int catalogWidth)
        {
            TableSize = tableSize;
            CellSize = cellSize;
            CellMargin = cellMargin;
            CatalogWidth = catalogWidth;
        }

        public PictureBox GenerateCellBlock(PictureBox sender, int x, int y)
        {
            var p = new PictureBox();
            //p.Image = (Image)((PictureBox)sender).Image.Clone();
            p.Name = "BlockPictureBox_";
            p.Image = ((PictureBox)sender).Image;
            p.Left = x * CellTableInterval + CellMargin;
            p.Top = y * CellTableInterval + CellMargin;
            p.Size = new System.Drawing.Size(CellSize, CellSize);
            p.SizeMode = PictureBoxSizeMode.Zoom;
            p.BorderStyle = BorderStyle.None;
            p.Cursor = Cursors.SizeAll;

            return p;
        }

        public PictureBox GenerateCellBlock(int graphicId, int x, int y)
        {
            var p = new PictureBox();
            //p.Image = (Image)((PictureBox)sender).Image.Clone();
            p.Name = "BlockPictureBox_";
            p.Image = Image.FromStream(File.OpenRead(HatoPath.FromAppDir(@"cells\cell_" +
                String.Format("{0:00000}", graphicId) + ".png")), false, false);
            p.Left = x * CellTableInterval + CellMargin;
            p.Top = y * CellTableInterval + CellMargin;
            p.Size = new System.Drawing.Size(CellSize, CellSize);
            p.SizeMode = PictureBoxSizeMode.Zoom;
            p.BorderStyle = BorderStyle.None;
            p.Cursor = Cursors.SizeAll;

            return p;
        }

        public PictureBox GenerateEmptyCellBlock(int cellId)  // 背景
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

            return p;
        }

        public PictureBox GenerateCatalog(int cellId, int graphicId)  // カタログ
        {
            var p = new PictureBox();
            //p.Image = Image.FromFile(@"cells\cell_0000" + (cellId + 1) + ".png");
            //p.ImageLocation = @"cells\cell_0000" + (cellId + 1) + ".png";
            p.Image = Image.FromStream(File.OpenRead(HatoPath.FromAppDir(@"cells\cell_" +
                String.Format("{0:00000}", graphicId) + ".png")), false, false);
            p.Name = "CellPreset_" + cellId;
            p.Left = cellId % CatalogWidth * 40 + 4;
            p.Top = cellId / CatalogWidth * 40 + 4;
            p.Size = new System.Drawing.Size(32, 32);
            p.SizeMode = PictureBoxSizeMode.Zoom;
            p.BorderStyle = BorderStyle.None;
            p.Cursor = Cursors.Hand;

            return p;
        }

        public PictureBox GenerateArrow(int arrowId2, int x1, int y1, bool isHorizontal)  // 矢印（と見せかけて三角形）
        {
            var size = CellMargin * 2;

            // true  -> 水平方向（左右向き）の矢印
            // false -> 垂直方向（左右向き）の矢印

            var p = new PictureBox();
            //p.Image = Image.FromFile(@"cells\arrow_00000.png");
            //p.ImageLocation = @"cells\arrow_00000.png";
            p.Image = Image.FromStream(File.OpenRead(@"cells\arrow_00000.png"), false, false);

            if (isHorizontal)
            {
                p.Name = "ArrowX_" + arrowId2;
                p.Left = (x1 + 1) * CellTableInterval - CellMargin;
                p.Top = y1 * CellTableInterval + CellTableInterval / 2 - CellMargin;
            }
            else
            {
                p.Name = "ArrowY_" + arrowId2;
                p.Left = x1 * CellTableInterval + CellTableInterval / 2 - CellMargin;
                p.Top = (y1 + 1) * CellTableInterval - CellMargin;
            }

            p.Size = new System.Drawing.Size(size, size);
            p.SizeMode = PictureBoxSizeMode.Zoom;
            p.BorderStyle = BorderStyle.None;
            p.Cursor = Cursors.Hand;

            return p;
        }
    }
}
