using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoSynthGUI
{
    class ArrowSummary
    {
        public int pos1x;  // 矢印の左または上
        public int pos1y;
        public int pos2x;  // 矢印の右または下
        public int pos2y;
        public ArrowDirection direction;

        public ArrowSummary(int x1, int y1, int x2, int y2)
        {
            Debug.Assert(x1 <= x2 && y1 <= y2);

            pos1x = x1;
            pos1y = y1;
            pos2x = x2;
            pos2y = y2;
            direction = ArrowDirection.None;
        }
    }
}
