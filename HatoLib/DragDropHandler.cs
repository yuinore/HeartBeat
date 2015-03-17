using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HatoLib
{
    // ＞ このサンプルは、別アプリケーション(ファイラ等)からのファイルドロップを受け付けるC#.NET版のサンプルです。
    // ＞ http://homepage2.nifty.com/nonnon/SoftSample/CS.NET/SampleFileDrop.html
    // 出来たぞ・・・まじでか・・・
    // 「WM_DROPFILES C#」でググったら出てきたんですけど・・・
    // ただ、64bitモードだとダメっぽいですね()
    // → SharpDXのコード見ながら修正しました
    //
    // あと関係ないけど、RenderFormを継承したら突然Visual Studioでフォームデザイナが表示されるようになって草

    public class DragDropHandler
    {
        // デリゲートの定義
        private delegate int D_DropWndProc(int hWnd, int uMsg, int wParam, int lParam);
        //public delegate int D_DropWndProc(int hWnd, int uMsg, int wParam, int lParam);
        D_DropWndProc DropWndProcPermanentInstance;

        // APIの定義
        private const int WM_DROPFILES = 0x233;
        private const int GWL_WNDPROC = -4;
        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        extern static void DragAcceptFiles(int hWnd, int fAccept);
        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        extern static void DragFinish(int HDROP);
        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        extern static int DragQueryFile(int HDROP, int UINT, StringBuilder lpStr, int ch);
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "CallWindowProcA")]
        extern static int CallWindowProc(IntPtr lpPrevWndFunc, int hWnd, int msg, int wParam, int lParam);
        //[System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongA")]
        //extern static int SetWindowLong(int hWnd, int nIndex, int dwNewLong);
        //[System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongA")]
        //extern static int SetWindowLong(int hWnd, int nIndex, D_DropWndProc dwNewLong);
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Unicode)]
        private static extern IntPtr SetWindowLong32(int hwnd, int nIndex, IntPtr dwNewLong);
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Unicode)]
        private static extern IntPtr SetWindowLong32(int hwnd, int nIndex, D_DropWndProc dwNewLong);
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Unicode)]
        private static extern IntPtr SetWindowLongPtr64(int hwnd, int nIndex, IntPtr dwNewLong);
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Unicode)]
        private static extern IntPtr SetWindowLongPtr64(int hwnd, int nIndex, D_DropWndProc dwNewLong);

        private IntPtr Msg_Proc;    // メッセージ関数アドレスの保存
        private int Hook_hWnd;   // ウインドウのハンドルの保存
        //private static DroppableForm _frm;     // 親ウィンドウオブジェクト
        public event EventHandler<string[]> OnDropFiles;

        Form form;

        public DragDropHandler(Form form)
        {
            DropWndProcPermanentInstance = DropWndProc;

            GC.KeepAlive(DropWndProcPermanentInstance);
            // https://msdn.microsoft.com/ja-jp/magazine/ee216332.aspx
            // しかし、ある時点でガベージ コレクタが実行され、SetWindowsHookEx P/Invoke メソッドに渡された
            // LowLevelKeyboardProc デリゲート インスタンスがどこからも参照されていないと認識されます。
            // この場合、GC は、デリゲートを再利用します (こうなるまで待つことができない場合には、
            // Application.Run の呼び出しの直前に GC.Collect の呼び出しを挿入し、
            // デリゲートを強制的にすぐに除去します)。この後、次に Windows がこのフックの使用を試みたとき、
            // アプリケーションは、ほぼ確実に、何の原因も明らかにしないまま停止します。

            form.Load += (o, e) => DropStart((int)form.Handle);

            form.FormClosed += (o, e) => DropEnd();

            this.form = form;
        }


        // 別アプリケーション(ファイラ等)からのドロップ許可開始
        private void DropStart(int hWnd)
        {
            if (hWnd != 0)
            {
                if (Msg_Proc != IntPtr.Zero) DropEnd();
                Hook_hWnd = hWnd;
                DragAcceptFiles(Hook_hWnd, 1);

                // 参考：https://github.com/sharpdx/SharpDX/blob/d0376184b166859c1fcc9dcbfb58cd780068d8b2/Source/SharpDX/Win32Native.cs

                if (IntPtr.Size == 4)
                {
                    Msg_Proc = SetWindowLong32(Hook_hWnd, GWL_WNDPROC, DropWndProcPermanentInstance);
                }
                else
                {
                    Msg_Proc = SetWindowLongPtr64(Hook_hWnd, GWL_WNDPROC, DropWndProcPermanentInstance);
                }
                //Msg_Proc = SetWindowLong(Hook_hWnd, GWL_WNDPROC, DropWndProc);
            }
        }

        // 別アプリケーション(ファイラ等)からのドロップ許可終了
        private void DropEnd()
        {
            if (Hook_hWnd != 0 && Msg_Proc != IntPtr.Zero)
            {
                if (IntPtr.Size == 4)
                {
                    SetWindowLong32(Hook_hWnd, GWL_WNDPROC, Msg_Proc);
                }
                else
                {
                    SetWindowLongPtr64(Hook_hWnd, GWL_WNDPROC, Msg_Proc);
                }
                //SetWindowLong(Hook_hWnd, GWL_WNDPROC, Msg_Proc);
            }
        }

        // メッセージ処理関数
        private int DropWndProc(int hWnd, int uMsg, int wParam, int lParam)
        {
            if (uMsg == WM_DROPFILES)
            {
                // ファイルドロップのメッセージ
                DropFilesProc(wParam);
                return 0;
            }
            else
            {
                return CallWindowProc(Msg_Proc, hWnd, uMsg, wParam, lParam);
            }
        }

        // ファイルドロップ処理関数
        private void DropFilesProc(int hDropFile)
        {
            try
            {
                // ドロップされたファイル/フォルダ数を取得
                int dropNum;
                dropNum = DragQueryFile(hDropFile, -1, null, 0);

                String[] dropFiles = new String[dropNum];

                for (int i = 0; i <= dropNum - 1; i++)
                {
                    // ドロップされたファイル/フォルダの取得
                    StringBuilder dropFile = new StringBuilder(512);
                    DragQueryFile(hDropFile, i, dropFile, 512);
                    dropFiles[i] = dropFile.ToString();

                    // ファイル/フォルダPATH文字列の後のNULLを削除
                    int p = dropFiles[i].IndexOf((char)0);
                    if (p >= 0) dropFiles[i] = dropFiles[i].Substring(0, p);
                }

                // ファイル/フォルダPATHをテキストボックスへ設定
                if (OnDropFiles != null)
                {
                    //Text1_DropFiles(dropFiles);
                    OnDropFiles(null, dropFiles);
                }

                // ドロップされたファイル/フォルダの取得終了
                DragFinish(hDropFile);
            }
            catch { }
        }

    }
}
