using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HatoPlayer
{
    internal class AsioHandler : IDisposable
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

            if (asiomain(Callback) == 0)  // dispose時にもう一度呼ぶのを忘れないで
            {
                System.Windows.Forms.MessageBox.Show("ASIOの初期化に失敗しました。");
                //throw new Exception("ASIOの初期化に失敗しました。");
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
            
            System.Diagnostics.Debug.Assert(disposing, "激おこ");

            if (disposing)
            {
                // Free any other managed objects here.
            }

            // Free any unmanaged objects here.
            asiomain(Callback);  // Dispose時にもう一度呼ぶことでASIOを停止する

            disposed = true;
        }
        
        ~AsioHandler()
        {
            Dispose(false);
        }
        #endregion
    }
}
