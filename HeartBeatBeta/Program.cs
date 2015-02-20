using HatoDraw;
using HatoWinLib;
using HeartBeatCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace HeartBeatBeta
{
    static class Program
    {
        static Form form;
        static BMSPlayer player;
        static int startmeasure = 0;
        static string filename = null;
        static bool playside2p_;
        static bool playside2p
        {
            get
            {
                return playside2p_;
            }
            set
            {
                playside2p_ = value;
                if (player != null)
                {
                    player.Playside2P = value;
                }
            }
        }
        static bool autoplay_ = true;
        static bool autoplay
        {
            get
            {
                return autoplay_;
            }
            set
            {
                autoplay_ = value;
                if (player != null)
                {
                    player.autoplay = value;
                }
            }
        }

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var prevproc = GetPreviousProcess();

            if (prevproc != null)
            {
                prevproc.Kill();
            }

            if (args.Length >= 3 && args[0] == "-S")
            {
                // コマンド：-S <LR2Path> <FileName> <options> を指定
                // 第一引数以外が-Sだった場合の動作は未定義
                // iBMSCの設定例：-S C:\(中略)\lr2body.exe <filename> -NS
                // Shiftキーを押しながら起動すると、オートプレイになります

                if ((Control.ModifierKeys & Keys.Shift) == 0)
                {
                    MessageBox.Show("\"" + args[2] + "\"" + String.Join(" ", args.Skip(3)));
                    var p = Process.Start(args[1], "\"" + args[2] + "\" " + String.Join(" ", args.Skip(3)));   // こんなに早く空白文字のバグが見つかるとはな・・・
                    p.WaitForExit();
                }
                else
                {
                    var p = Process.Start(args[1], "\"" + args[2] + "\" " + String.Join(" ", args.Skip(3)) + " -A");
                    p.WaitForExit();
                }

                return;
            }

            foreach (var cmd in args)
            {
                if (cmd == "-P")
                {
                }
                else if (cmd.Length >= 2 && cmd[0] == '-' && cmd[1] == 'N')
                {
                    startmeasure = Convert.ToInt32(cmd.Substring(2));
                }
                else if (cmd.Length >= 1 && cmd[0] != '-')
                {
                    // 空白を含む場合は？→なぜか普通に実行できて怖い
                    filename = cmd;
                }
            }

            player = new BMSPlayer();
            form = player.OpenForm();

            // ファイルのドロップ設定
            var c = new DragDropHandler(form);
            c.OnDropFiles += form_DropFiles;

            if (filename != null)
            {
                if ((Control.ModifierKeys & Keys.Shift) != 0) player.autoplay = false;  // 2曲目移行でShiftが押されていなければ、現在のモードのまま
                player.Playside2P = playside2p;
                player.autoplay = autoplay;
                player.LoadAndPlay(filename, startmeasure);
            }

            form.KeyDown += (o, e) =>
            {
                if (e.KeyCode == Keys.P && e.Control)
                {
                    playside2p = !playside2p;
                }
                if (e.KeyCode == Keys.A && e.Control)
                {
                    autoplay = !autoplay;
                }
            };

            player.Run();

            /*hdraw.Start(
            (rt) =>
            {
            },
            (rt) =>
            {
            });*/

        }

        // C#(.net)で他のプロセスのメインウィンドウハンドルを取得する
        // http://tomoemon.hateblo.jp/entry/20080430/p2
        public static Process GetPreviousProcess()
        {
            Process curProcess = Process.GetCurrentProcess();
            Process[] allProcesses =
              Process.GetProcessesByName(curProcess.ProcessName);

            foreach (Process checkProcess in allProcesses)
            {
                // 自分自身のプロセスIDは無視する
                if (checkProcess.Id != curProcess.Id)
                {
                    string prev = checkProcess.MainModule.FileName;
                    string cur = curProcess.MainModule.FileName;
                    if (String.Compare(prev, cur, true) == 0)
                    {
                        // 起動済みで同じフルパス名のプロセスを取得
                        return checkProcess;
                    }
                }
            }
            return null;
        }


        // テキストボックスへのファイルドロップ処理
        public static void form_DropFiles(object sender, String[] files)
        {
            // ファイル/フォルダPATHをテキストボックスへ設定
            // このサンプルは１個だけ
            if (files.Length > 0)
            {
                Console.WriteLine(files[0]);

                startmeasure = 0;
                player.autoplay = ((Control.ModifierKeys & Keys.Shift) == 0);
                player.Playside2P = playside2p;
                player.autoplay = autoplay;
                player.LoadAndPlay(files[0], startmeasure);
            }
        }
    }

}
