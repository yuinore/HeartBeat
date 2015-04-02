using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HatoPlayer
{
    internal class AsioHandler
    {
        internal delegate void D_AsioCallback(IntPtr buf, int chIdx, int count);

        // DragDropHandlerを参考にして書いた
        //
        // CallingConvention 列挙体 - MSDN
        // https://msdn.microsoft.com/ja-jp/library/system.runtime.interopservices.callingconvention%28v=vs.110%29.aspx
        //
        // アンマネージコードにC#のデリゲートを渡す - aharisuのごみ箱
        // http://d.hatena.ne.jp/aharisu/20090401/1238561406
        [DllImport("ASIOHost.dll", CallingConvention = CallingConvention.StdCall)]
        private extern static int asiomain(D_AsioCallback Callback);

        D_AsioCallback Callback;

        /*unsafe void MyCallback(IntPtr buf, int chIdx, int count)
        {
            short* p = (short*)buf;
            for (int i = 0; i < count; i++)
            {
                *(p++) = 0;
                *(p++) = 0;  // data
            }
        }*/

        public void Run(D_AsioCallback callback)
        {
            Callback = callback;

            GC.KeepAlive(Callback);

            Console.WriteLine("今からASIO初期化に行きます");
            asiomain(Callback);  // dispose時にもう一度呼ぶのを忘れないで
        }

        ~AsioHandler()
        {
            asiomain(Callback);  // dispose時にもう一度呼ぶのを忘れないで
        }
    }
}
