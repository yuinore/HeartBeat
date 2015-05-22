using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HatoSynthGUI
{
    class ArrowSummary
    {
        public int pos1x;  // 矢印の左または上
        public int pos1y;
        public int pos2x;  // 矢印の右または下
        public int pos2y;
        private ArrowDirection _direction;
        private readonly bool _horizontal;
        public readonly PictureBox pBox;

        public ArrowSummary(PictureBox p, int x1, int y1, int x2, int y2)
        {
            Debug.Assert(x1 <= x2 && y1 <= y2);

            pos1x = x1;
            pos1y = y1;
            pos2x = x2;
            pos2y = y2;
            direction = ArrowDirection.None;
            pBox = p;
            _horizontal = (y1 == y2);
        }

        public ArrowDirection direction
        {
            get
            {
                return _direction;
            }
            set
            {
                if (_direction != value)
                {
                    _direction = value;

                    switch (_direction)
                    {
                        case ArrowDirection.None:
                            pBox.ImageLocation = @"cells\arrow_00000.png"; break;
                        case ArrowDirection.Up:
                            pBox.ImageLocation = @"cells\arrow_00001.png"; break;
                        case ArrowDirection.Right:
                            pBox.ImageLocation = @"cells\arrow_00002.png"; break;
                        case ArrowDirection.Down:
                            pBox.ImageLocation = @"cells\arrow_00003.png"; break;
                        case ArrowDirection.Left:
                            pBox.ImageLocation = @"cells\arrow_00004.png"; break;
                        case ArrowDirection.UpAlt:
                            pBox.ImageLocation = @"cells\arrow_00005.png"; break;
                        case ArrowDirection.RightAlt:
                            pBox.ImageLocation = @"cells\arrow_00006.png"; break;
                        case ArrowDirection.DownAlt:
                            pBox.ImageLocation = @"cells\arrow_00007.png"; break;
                        case ArrowDirection.LeftAlt:
                            pBox.ImageLocation = @"cells\arrow_00008.png"; break;
                    }
                }
            }
        }

        public void Toggle()
        {
            if (_horizontal)
            {
                if (direction == ArrowDirection.None)
                {
                    direction = ArrowDirection.Right;
                }
                else if (direction == ArrowDirection.Right)
                {
                    direction = ArrowDirection.Left;
                }
                else if (direction == ArrowDirection.Left)
                {
                    direction = ArrowDirection.RightAlt;
                }
                else if (direction == ArrowDirection.RightAlt)
                {
                    direction = ArrowDirection.LeftAlt;
                }
                else
                {
                    direction = ArrowDirection.None;
                }
            }
            else
            {
                if (direction == ArrowDirection.None)
                {
                    direction = ArrowDirection.Down;
                }
                else if (direction == ArrowDirection.Down)
                {
                    direction = ArrowDirection.Up;
                }
                else if (direction == ArrowDirection.Up)
                {
                    direction = ArrowDirection.DownAlt;
                }
                else if (direction == ArrowDirection.DownAlt)
                {
                    direction = ArrowDirection.UpAlt;
                }
                else
                {
                    direction = ArrowDirection.None;
                }
            }
        }
    }
}
