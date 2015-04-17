using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HatoPlayer
{
    public class AsioHandler : IDisposable  // TODO: unsafeを使わずにもう少しマシにAsioHandlerを呼び出せるようにする
    {
        internal enum AsioSampleType
        {
            ASIOSTInt16MSB = 0,
            ASIOSTInt24MSB = 1,		// used for 20 bits as well
            ASIOSTInt32MSB = 2,
            ASIOSTFloat32MSB = 3,		// IEEE 754 32 bit float
            ASIOSTFloat64MSB = 4,		// IEEE 754 64 bit double float

            // these are used for 32 bit data buffer, with different alignment of the data inside
            // 32 bit PCI bus systems can be more easily used with these
            ASIOSTInt32MSB16 = 8,		// 32 bit data with 16 bit alignment
            ASIOSTInt32MSB18 = 9,		// 32 bit data with 18 bit alignment
            ASIOSTInt32MSB20 = 10,		// 32 bit data with 20 bit alignment
            ASIOSTInt32MSB24 = 11,		// 32 bit data with 24 bit alignment

            ASIOSTInt16LSB = 16,
            ASIOSTInt24LSB = 17,		// used for 20 bits as well
            ASIOSTInt32LSB = 18,
            ASIOSTFloat32LSB = 19,		// IEEE 754 32 bit float, as found on Intel x86 architecture
            ASIOSTFloat64LSB = 20, 		// IEEE 754 64 bit double float, as found on Intel x86 architecture

            // these are used for 32 bit data buffer, with different alignment of the data inside
            // 32 bit PCI bus systems can more easily used with these
            ASIOSTInt32LSB16 = 24,		// 32 bit data with 18 bit alignment
            ASIOSTInt32LSB18 = 25,		// 32 bit data with 18 bit alignment
            ASIOSTInt32LSB20 = 26,		// 32 bit data with 20 bit alignment
            ASIOSTInt32LSB24 = 27,		// 32 bit data with 24 bit alignment

            //	ASIO DSD format.
            ASIOSTDSDInt8LSB1 = 32,		// DSD 1 bit data, 8 samples per byte. First sample in Least significant bit.
            ASIOSTDSDInt8MSB1 = 33,		// DSD 1 bit data, 8 samples per byte. First sample in Most significant bit.
            ASIOSTDSDInt8NER8 = 40,		// DSD 8 bit data, 1 sample per byte. No Endianness required.

            ASIOSTLastEntry
        }

        /// <summary>
        /// ASIOへオーディオデータを返すためのバッファーを示します。
        /// </summary>
        // デリゲートを作るのが面倒だったが、デリゲートを作らないと変数名を明示できなかったため、こうなった。
        public struct AsioBuffer
        {
            public readonly float[][] Buffer;
            public readonly int ChannelCount;
            public readonly int SampleCount;

            public AsioBuffer(float[][] buffer, int ch, int count)
            {
                Buffer = buffer;
                ChannelCount = ch;
                SampleCount = count;
            }
        }

        internal delegate void D_UnsafeAsioCallback(IntPtr buf, int chIdx, int count);
        //public delegate void D_AsioCallback(float[][] buf, int channelCount, int sampleCount);
        // メモ：channelsCountではなくchannelCountの方が英語として良さそう

        // DragDropHandlerを参考にして書いた
        //
        // CallingConvention 列挙体 - MSDN
        // https://msdn.microsoft.com/ja-jp/library/system.runtime.interopservices.callingconvention%28v=vs.110%29.aspx
        //
        // アンマネージコードにC#のデリゲートを渡す - aharisuのごみ箱
        // http://d.hatena.ne.jp/aharisu/20090401/1238561406
        [DllImport("ASIOHost.dll", CallingConvention = CallingConvention.StdCall)]
        private extern static int asiomain(D_UnsafeAsioCallback Callback);

        D_UnsafeAsioCallback UnsafeCallback;
        Action<AsioBuffer> SafeCallback;

        float[][] buffer = new float[2][] {  // bufferを保管しておき、毎回バッファを作成することを防ぐ。
                new float[1024],
                new float[1024],
        };

        bool LeftRemains = false;  // ASIOに送信していない左チャンネルのデータがbufferにまだ残っているか？
        bool RightRemains = false;  // ASIOに送信していない右チャンネルのデータがbufferにまだ残っているか？
        int chLeft = 2;  // TODO: チャンネル番号の設定
        int chRight = 3;

        private unsafe void UnsafeAsioCallback(IntPtr buf, int chIdx, int count)
        {
            if (buffer.Length < count)
            {
                // マネージド配列の長さが足りない場合
                buffer = new float[2][] {
                    new float[count],
                    new float[count]
                };  // 要素数は適当(count以上ならなんでもよい)、チャンネル数は2固定（5.1サラウンドなんて無かった）
            }

            if (chIdx != chLeft && chIdx != chRight) return;  // データを書き込むべきチャンネルでなければ何もしない

            //*** 必要に応じてSafeCallBackからのデータのfetch
            if ((chIdx == chLeft && !LeftRemains) || (chIdx == chRight && !RightRemains))
            {
                SafeCallback(new AsioBuffer(buffer, 2, count));  // チャンネル数は2
                LeftRemains = RightRemains = true;
            }

            //*** コピーするチャンネルの選択
            float[] currChBuf;

            if (chIdx == chLeft)
            {
                currChBuf = buffer[0];
                LeftRemains = false;
            }
            else
            {
                currChBuf = buffer[1];
                RightRemains = false;
            }

            //*** ASIOバッファへのコピー
            // FIXME: AsioSampleTypeによるswitch
            short* p = (short*)buf;
            for (int i = 0; i < count; i++)
            {
                double fsample = currChBuf[i] * 3276.7;  // FIXME:音量調整
                short ssample = (short)fsample;
                if(fsample > 32767.0) ssample = 32767;
                if(fsample < -32768.0) ssample = -32768;
                *(p++) = 0;
                *(p++) = ssample;
            }
        }

        /// <summary>
        /// コールバック関数を指定して、ASIOサウンドドライバを立ち上げます。
        /// オーディオ信号処理は、ふつう別のスレッドで処理されることに注意して下さい。
        /// コールバック関数の引数として与えられる AsioBuffer の float[][] Buffer メンバは、
        /// その配列の長さが要求されたサンプル数より長い場合があるということに注意して下さい。
        /// </summary>
        public void Run(Action<AsioBuffer> callback)
        {
            SafeCallback = callback;

            Run(new D_UnsafeAsioCallback(UnsafeAsioCallback));
        }

        /// <summary>
        /// コールバック関数を指定して、ASIOサウンドドライバを立ち上げます。
        /// 可能ならばこの関数は使用せず、代わりに void AsioHandler.Run(Action&lt;AsioBuffer&gt; callback) を使用して下さい。
        /// </summary>
        internal void Run(D_UnsafeAsioCallback callback)  // TODO:最終的にはinternalをprivateにする
        {
            UnsafeCallback = callback;

            GC.KeepAlive(UnsafeCallback);

            Console.WriteLine("今からASIO初期化に行きます");

            if (asiomain(UnsafeCallback) == 0)  // dispose時にもう一度呼ぶのを忘れないで
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
            asiomain(UnsafeCallback);  // Dispose時にもう一度呼ぶことでASIOを停止する

            disposed = true;
        }
        
        ~AsioHandler()
        {
            Dispose(false);
        }
        #endregion
    }
}
