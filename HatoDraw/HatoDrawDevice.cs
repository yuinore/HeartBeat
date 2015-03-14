using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Device = SharpDX.Direct3D11.Device;
using FactoryD2D = SharpDX.Direct2D1.Factory;
using FactoryDXGI = SharpDX.DXGI.Factory1;
using D2D = SharpDX.Direct2D1;
using System.Windows.Forms;
using SharpDX;

namespace HatoDraw
{
    // program bone from: "C# DirectX API Face-off: SlimDX vs SharpDX – Which should you choose?"
    // https://katyscode.wordpress.com/2013/08/24/c-directx-api-face-off-slimdx-vs-sharpdx-which-should-you-choose/

    /// <summary>
    /// Direct2Dを使用して画面に描画するクラス。ラッパーではない。
    /// </summary>
    public class HatoDrawDevice : IDisposable
    {
        /// <summary>
        /// スキンに固有の値の、描画領域の幅(dip)
        /// </summary>
        public float DeviceIndependentWidth = 640;

        /// <summary>
        /// スキンに固有の値の、描画領域の高さ(dip)
        /// </summary>
        public float DeviceIndependentHeight = 480;

        /// <summary>
        /// ウィンドウモード時の描画領域の幅(pixels)
        /// </summary>
        public float ClientWidth = 640;

        /// <summary>
        /// ウィンドウモード時の描画領域の高さ(pixels)
        /// </summary>
        public float ClientHeight = 480;
        /// <summary>
        /// 描画の頻度。0を指定すると可能な限り高い頻度で描画し、n(1&lt;=n&lt;=4)を指定すると、リフレッシュレートの1/n倍の頻度で描画します。
        /// </summary>
        public int SyncInterval = 1;

        /// <summary>
        /// アンチエイリアスの量。1でアンチエイリアス無し、2以上でアンチエイリアス有り。<br></br>
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/bb173075%28v=vs.85%29.aspx <br></br>
        /// この値は、Start()が呼ばれる前に設定される必要があります。
        /// </summary>
        public int SampleDescCount = 1;

        /// <summary>
        /// 画面モードの設定。<br></br>
        /// この値は、Start()が呼ばれる前に設定される必要があります。
        /// </summary>
        public bool IsWindowed = true;

        /// <summary>
        /// フォームウィンドウの説明文。<br></br>
        /// この値は、Start()が呼ばれる前に設定される必要があります。
        /// </summary>
        public string Caption = "HatoDraw Form";

        /// <summary>
        /// dipをpxに変換するためのdpiです。0を指定すると規定の値を使用します。
        /// なんかよくわかりません。
        /// </summary>
        public float DPI = 0f;

        private Device device;
        private SwapChain swapChain;
        private D2D.RenderTarget d2dRenderTarget;

        public RenderTarget HatoRenderTarget
        {
            get;
            private set;
        }


        bool started = false;

        /// <summary>
        /// HatoDrawのインスタンスを作成し、Windowsフォームを作成する準備を行います。
        /// </summary>
        public HatoDrawDevice()
        {
        }

        RenderForm form;

        /// <summary>
        /// 同期的にフォームを開きます。
        /// Run()を呼び出すまで、レンダリングループは開始されません。
        /// </summary>
        public Form OpenForm()
        {
            form = new RenderForm(Caption);
            // 同じスレッドで処理されることを保証できるか？
            // レンダリング対象のウィンドウを作成

            // Set window size
            //form.Size = new System.Drawing.Size(640, 480);  // ←まちがい
            form.ClientSize = new System.Drawing.Size((int)ClientWidth, (int)ClientHeight);  // ←せいかい！

            // Prevent window from being re-sized
            form.AutoSizeMode = AutoSizeMode.GrowOnly;

            // swap chain description を作成する
            var swapChainDesc = new SwapChainDescription()
            {
                BufferCount = 2,  // バッファの数。通常は2
                Usage = Usage.RenderTargetOutput,  // スワップチェインの使用目的
                OutputHandle = form.Handle,  // 出力対象のウィンドウ
                IsWindowed = IsWindowed,  // ウィンドウモードか
                ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                SampleDescription = new SampleDescription(SampleDescCount, 0),  // アンチエイリアスの設定
                Flags = SwapChainFlags.AllowModeSwitch,
                SwapEffect = SwapEffect.Discard
            };

            // swap chain と Direct3D device を作成する
            // The BgraSupport flag is needed for Direct2D compatibility otherwise new RenderTarget() will fail!
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, swapChainDesc, out device, out swapChain);

            // Get back buffer in a Direct2D-compatible format (DXGI surface)
            Surface backBuffer = Surface.FromSwapChain(swapChain, 0);

            // Direct2D factory を作成する
            using (var factory = new FactoryD2D())
            {
                // Get desktop DPI
                var renderDpi = factory.DesktopDpi;
                //Console.WriteLine("Desktop dpi : " + renderDpi.Width + "," + renderDpi.Height);
                //dpi = new Size2F(dpi.Width * 1.25f, dpi.Height * 1.25f);  // ここやばい
                if (DPI != 0)
                {
                    renderDpi = new Size2F(DPI * ClientWidth / DeviceIndependentWidth, DPI * ClientHeight / DeviceIndependentHeight);
                }

                // Create bitmap render target from DXGI surface
                d2dRenderTarget = new D2D.RenderTarget(factory, backBuffer, new RenderTargetProperties()
                {
                    DpiX = renderDpi.Width,  // この値が何か重要な働きをしている・・・
                    DpiY = renderDpi.Height,
                    MinLevel = SharpDX.Direct2D1.FeatureLevel.Level_DEFAULT,
                    PixelFormat = new PixelFormat(Format.Unknown, AlphaMode.Ignore),
                    //PixelFormat = new PixelFormat(Format.R8G8B8A8_UInt, AlphaMode.Straight),
                    Type = RenderTargetType.Default,
                    Usage = RenderTargetUsage.None
                });

                HatoRenderTarget = new RenderTarget(d2dRenderTarget);
            }

            // Disable automatic ALT+Enter processing because it doesn't work properly with WinForms
            using (var factory = swapChain.GetParent<FactoryDXGI>())   // Factory or Factory1?
                factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAltEnter);

            // Add event handler for ALT+Enter
            form.KeyDown += (o, e) =>
            {
                if (e.Alt && e.KeyCode == Keys.Enter)
                {
                    swapChain.IsFullScreen = !swapChain.IsFullScreen;
                }
            };

            return form;
        }

        /// <summary>
        /// レンダーループを同期的に開始します。
        /// DirectXフォームが閉じられると、処理が返ります。
        /// </summary>
        public void Start(Action<RenderTarget> onLoad, Action<RenderTarget> onPaint)
        {
            if (started) return;  // 適当

            started = true;

            onLoad(HatoRenderTarget);

            // Rendering function
            RenderLoop.Run(form, () =>
            {
                d2dRenderTarget.BeginDraw();
                d2dRenderTarget.Transform = Matrix3x2.Identity;

                onPaint(HatoRenderTarget);

                d2dRenderTarget.EndDraw();

                swapChain.Present(SyncInterval, PresentFlags.None);
            });

            Dispose();
        }

        //********* implementation of IDisposable *********//

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

            if (disposing)
            {
                // Free any other managed objects here.
                d2dRenderTarget.Dispose();
                swapChain.Dispose();
                device.Dispose();
            }
            else
            {
                try
                {
                    // ユーザーコード内でDisposeされなかったら文句を言う（デバッグ時のみ）
                    throw new Exception("激おこ @ HatoDrawForm.Dispose(bool)");
                }
                catch
                {
                }
            }

            // Free any unmanaged objects here.

            disposed = true;
        }

        //********* implementation of IDisposable *********//
    }
}
