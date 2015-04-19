using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class CellWire
    {
        /// <summary>
        /// セル同士の接続の種類を表します。
        /// 黒三角が 0 で、白三角が 1 です。
        /// ただし、ミキサーなど、この限りではない場合もあります。
        /// </summary>
        public readonly int Port;  // connection type, 0, 1, ...

        public readonly CellTree Source;

        public CellWire(CellTree src, int port)
        {
            this.Source = src;
            this.Port = port;
        }
    }
}
