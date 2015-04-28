using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HatoSynthGUI
{
    /// <summary>
    /// ブロックの表を管理するクラスです。
    /// スレッドセーフではないので、asyncとか使うときは修正して下さい。
    /// </summary>
    class BlockTableManager
    {
        // block と cell と module の厳密な単語の使い分けが為されていないのが気になる・・・
        // Cell と CellTree の命名も微妙だし

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

        /// <summary>
        /// Blockの表を管理します。
        /// sizeには、表の縦と横の向きのブロックの個数を指定します。
        /// </summary>
        public BlockTableManager(System.Drawing.Size size)
        {
            TableSize = size;
            table = new CellBlock[TableSize.Height, TableSize.Width];
        }

        CellBlock[,] table;
        System.Drawing.Size TableSize;

        /// <summary>
        /// tableをPictureBoxで逆引きします。
        /// 見つからなかった場合は例外をスローします。
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

        /// <summary>
        /// すべてのセルが埋まっているかどうかを返します。
        /// 埋まっていなかった場合は、空のセルからひとつを選び、
        /// posx, posy にその位置を格納して返します。
        /// </summary>
        /// <param name="posx"></param>
        /// <param name="posy"></param>
        /// <returns></returns>
        public bool IsFull(out int posx, out int posy)
        {
            int x = 0, y = 0;

            for (y = 0; y < TableSize.Height; y++)
            {
                for (x = 0; x < TableSize.Width; x++)
                {
                    if (table[y, x] == null)
                    {
                        posx = x;
                        posy = y;
                        return false;
                    }
                }
            }

            posx = posy = -1;
            return true;
        }

        /// <summary>
        /// ブロックを挿入します。
        /// 空ではない位置に挿入しようとすると、例外をスローします。
        /// </summary>
        public void Add(PictureBox p, int x, int y, BlockPresetLibrary.BlockPreset preset)
        {
            if (table[y, x] != null) throw new ArgumentException("空ではない位置にセルを挿入しようとしました。");

            var cb = new CellBlock();

            cb.pBox = p;
            cb.x = x;
            cb.y = y;
            cb.preset = preset;

            table[y, x] = cb;
        }

        /// <summary>
        /// ブロックを posx, posy で指定した場所に移動します。
        /// 移動できた場合は true を返し、 posx, posy には移動先の位置が格納されます。
        /// もし移動できなかった場合は false を返し、 posx, posy には移動前の位置が格納されます。
        /// </summary>
        public bool TryMove(PictureBox draggingBox, ref int posx, ref int posy)
        {
            CellBlock cb = null;

            if (table[posy, posx] == null)
            {
                cb = pictureboxToCellblock(draggingBox);

                table[cb.y, cb.x] = null;

                cb.x = posx;
                cb.y = posy;
                table[posy, posx] = cb;

                return true;
            }
            else
            {
                cb = pictureboxToCellblock(draggingBox);

                posx = cb.x;  // **返り値**
                posy = cb.y;
                return false;
            }
        }

        public bool TryGetPreset(int x, int y, out BlockPresetLibrary.BlockPreset preset)
        {
            // ?.演算子、今、ほんのちょっとだけ欲しいと思った
            // return table[x,y]?.preset;

            if (table[y, x] == null)
            {
                preset = null;
                return false;
            }
            else
            {
                preset = table[y, x].preset;
                return true;
            }
        }

        public void Remove(PictureBox pBox)
        {
            CellBlock cb = pictureboxToCellblock(pBox);
            table[cb.y, cb.x] = null;
        }
    }
}
