using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoBMSLib
{
    class BMSFormatException : Exception
    {
        public BMSFormatException()
            : base("BMSファイルの書式に誤りがある可能性があります。")
        {
        }

        public BMSFormatException(string message)
            : base(message)
        {
        }

        public BMSFormatException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
