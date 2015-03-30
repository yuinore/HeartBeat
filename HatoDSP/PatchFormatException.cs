using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    class PatchFormatException : Exception
    {
        public PatchFormatException()
            : base("HatoSynthのパッチファイルの書式に誤りがある可能性があります。")
        {
        }

        public PatchFormatException(string message)
            : base("HatoSynthのパッチファイルの書式に誤りがある可能性があります：\n" + message)
        {
        }

        public PatchFormatException(string message, Exception inner)
            : base("HatoSynthのパッチファイルの書式に誤りがある可能性があります：\n" + message, inner)
        {
        }
    }
}
