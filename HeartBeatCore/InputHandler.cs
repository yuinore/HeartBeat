using HatoBMSLib;
using HatoLib;
using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HeartBeatCore
{
    internal class InputHandler : IDisposable
    {
        /// <summary>
        /// キーが押されたときに発生します。第２引数は対応するkeyidです。
        /// このイベントは、キーが押したままにされていても１度しか発生しません。
        /// このイベントの中で、キー音の再生を行います。
        /// あまり重い処理をここに書かない方がいいかもしれません。
        /// </summary>
        public EventHandler<int> KeyDown;  // キー音の再生等を行う

        /// <summary>
        /// キーが離されたときに発生します。第２引数は対応するkeyidです。
        /// このイベントの中で、キー音の停止を行います。
        /// </summary>
        public EventHandler<int> KeyUp;

        /// <summary>
        /// 各キーidに対応する、最後に押したキーの時刻。キーフラッシュ用。
        /// </summary>
        public Dictionary<int, double> LastKeyDownEventDict = new Dictionary<int, double>();

        // キー入力キューで、フレームごとに消化される。（描画に直接用いることはない）
        public Queue<KeyEvent> KeyEventList = new Queue<KeyEvent>();

        /// <summary>
        /// 最後に押されたキーとその時刻を表します。
        /// </summary>
        public KeyEvent LastKeyDownEvent;

        Form form;
        BMSPlayer player;

        // キーが押されている状態かどうかを示すbool変数
        Dictionary<int, bool> isKeyDown = new Dictionary<int, bool>();

        InputDevice midiInDev;

        public InputHandler(BMSPlayer player, Form form)
        {
            this.form = form;
            this.player = player;

            // フォームへのイベントハンドラの登録
            form.KeyDown += form_KeyDown;
            form.KeyUp += form_KeyUp;

            // Midi入力デバイスの列挙
            for (int i = 0; i < InputDevice.DeviceCount; i++)
            {
                var dev = InputDevice.GetDeviceCapabilities(i);

                Console.WriteLine("in " + i + " : " + dev.name);
            }

            // midi入力の初期化
            if(InputDevice.DeviceCount >= 9) {
                midiInDev = new InputDevice(8);  // Windowsからmidiデバイスを開く
                midiInDev.ChannelMessageReceived += midiInDev_ChannelMessageReceived;  // コールバック関数の指定
                midiInDev.StartRecording();  // 入力待機の開始
            }
        }

        #region 一般のキー入力処理
        /// <summary>
        /// 一般のキーダウンが発生した場合に呼び出します。
        /// </summary>
        void AnyKeyDown(object sender, int keyid)
        {
            // TODO: midiキーとPCキーボードが同時に押されてもイベントを重複して発生させない

            double CSP = player.CurrentSongPosition();

            LastKeyDownEvent = new KeyEvent
            {
                keyid = (int)keyid,
                seconds = CSP
            };
            lock (LastKeyDownEventDict)
            {
                LastKeyDownEventDict[(int)keyid] = CSP;
            }

            lock (KeyEventList)
            {
                KeyEventList.Enqueue(LastKeyDownEvent);
            }

            KeyDown(sender, (int)keyid);
        }

        /// <summary>
        /// 一般のキーアップが発生した場合に呼び出します。
        /// </summary>
        void AnyKeyUp(object sender, int keyid)
        {
            lock (KeyEventList)
            {
                KeyEventList.Enqueue(new KeyEvent
                {
                    IsKeyUp = true,
                    keyid = (int)keyid,
                    seconds = player.CurrentSongPosition()
                });
            }

            KeyUp(sender, (int)keyid);
        }
        #endregion

        #region PCキーボード入力のイベントハンドラ
        void form_KeyDown(object sender, KeyEventArgs ev)
        {
            int? keyid = KeyCodeToKeyid(ev.KeyCode);
            bool cond = false;

            lock (isKeyDown)  // デッドロックに注意
            {
                if (keyid != null && isKeyDown.GetValueOrDefault((int)keyid) == false)
                {
                    isKeyDown[(int)keyid] = true;
                    cond = true;
                }
            }

            if (cond)
            {
                AnyKeyDown(sender, (int)keyid);
            }
        }

        void form_KeyUp(object sender, KeyEventArgs ev)
        {
            int? keyid = KeyCodeToKeyid(ev.KeyCode);
            bool cond = false;

            lock (isKeyDown)  // デッドロックに注意
            {
                if (keyid != null && isKeyDown.ContainsKey((int)keyid))
                {
                    isKeyDown[(int)keyid] = false;  // removeでも良い感
                    cond = true;
                }
            }

            if (cond)
            {
                AnyKeyUp(sender, (int)keyid);
            }
        }

        /// <summary>
        /// KeyCodeをkeyidに変換します。
        /// </summary>
        int? KeyCodeToKeyid(Keys KeyCode)  // 現状だと1つのキーに複数のkeyidを割り当てることは出来ない（出来なくていいと思うけど）
        {
            int? keyid = null;

            if (KeyCode == Keys.Z) { keyid = 1; }
            if (KeyCode == Keys.S) { keyid = 2; }
            if (KeyCode == Keys.X) { keyid = 3; }
            if (KeyCode == Keys.D) { keyid = 4; }
            if (KeyCode == Keys.C) { keyid = 5; }
            if (KeyCode == Keys.F) { keyid = 8; }
            if (KeyCode == Keys.V) { keyid = 9; }
            if (!player.Playside2P)
            {
                if (KeyCode == Keys.ShiftKey) { keyid = 6; }
            }
            else
            {
                if (KeyCode == Keys.B) { keyid = 6; }
            }

            return keyid;
        }
        #endregion

        #region Midi入力のイベントハンドラ
        void midiInDev_ChannelMessageReceived(object sender, ChannelMessageEventArgs ev)
        {
            ChannelCommand cmd = ev.Message.Command;
            int n = ev.Message.Data1;  // ノート番号
            int vel = ev.Message.Data2;  // ベロシティ（ノートオン時のみ）

            switch (cmd)
            {
                case ChannelCommand.NoteOn:
                    AnyKeyDown(sender, MidiNoteNumberToKeyid(n));
                    break;
                case ChannelCommand.NoteOff:
                    AnyKeyUp(sender, MidiNoteNumberToKeyid(n));
                    break;
            }
        }

        int MidiNoteNumberToKeyid(int n)  // 現状だと1つのキーに複数のkeyidを割り当てることは出来ない（出来なくていいと思うけど）
        {
            return (new[] { 1, 6, 3, 6, 5, 9, 2, 6, 4, 6, 8, 6, 6 })[n % 12];
        }
        #endregion

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
            
            System.Diagnostics.Debug.Assert(disposing, "激おこ @ " + this.GetType().ToString());

            if (disposing)
            {
                // Free any other managed objects here.
                midiInDev.StopRecording();
                midiInDev.Close();
                midiInDev.Dispose();  // 不要かも?
                midiInDev = null;
            }

            // Free any unmanaged objects here.

            disposed = true;
        }
        
        ~InputHandler()
        {
            Dispose(false);
        }
        #endregion
    }
}
