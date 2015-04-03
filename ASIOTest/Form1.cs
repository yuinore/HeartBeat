using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASIOTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private delegate void D_AsioCallback(IntPtr buf, int chIdx, int count);

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

        Stopwatch s = new Stopwatch();
        long last = 0;

        Dictionary<int, long> ns = new Dictionary<int, long>();

        unsafe void MyCallback(IntPtr buf, int bufIdx, int count)
        {
            Console.WriteLine("buf " + bufIdx + " req:" + count + ", " + (s.ElapsedMilliseconds - last));
            last = s.ElapsedMilliseconds;

            //int j = bufIdx * 256 + chIdx;
            int j = bufIdx;

            if (!ns.ContainsKey(j)) ns[j] = 0;

            short* p = (short*)buf;
            for (int i = 0; i < count; i++, ns[j]++)
            {
                *(p++) = 0;
                *(p++) = (short)(Math.Sin(ns[j] * 0.1 + Math.Sin(0.0001 * ns[j]) * 100) * 32767);
                //*(p++) = (short)(ns[j] % 105 * 277);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            s.Start();

            Callback = MyCallback;

            GC.KeepAlive(Callback);

            asiomain(Callback);

            await Task.Delay(1000);

            asiomain(Callback);
        }
    }
}
