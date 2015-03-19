using HatoBMSLib;
using HatoLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HeartBeatCore
{
    internal class InputHandler
    {
        /// <summary>
        /// キーが押されたときに発生します。第２引数は対応するkeyidです。
        /// このイベントは、キーが押したままにされていても１度しか発生しません。
        /// </summary>
        public EventHandler<int> KeyDown;  // キー音の再生等を行う
        /// <summary>
        /// キーが離されたときに発生します。第２引数は対応するkeyidです。
        /// </summary>
        public EventHandler<int> KeyUp;

        // 各キーidに対応する、最後に押したキーの時刻。キーフラッシュ用。
        public Dictionary<int, double> LastKeyEventDict = new Dictionary<int, double>();

        // キー入力キューで、フレームごとに消化される。（描画に直接用いることはない）
        public Queue<KeyEvent> KeyEventList = new Queue<KeyEvent>();

        /// <summary>
        /// 最後に押されたキーとその時刻を表します。
        /// </summary>
        public KeyEvent LastKeyEvent;

        Form form;
        BMSPlayer player;

        // キーが押されている状態かどうかを示すbool変数
        Dictionary<int, bool> isKeyDown = new Dictionary<int, bool>();

        public InputHandler(BMSPlayer player, Form form)
        {
            this.form = form;
            this.player = player;

            form.KeyDown += form_KeyDown;
            form.KeyUp += form_KeyUp;
        }

        void form_KeyUp(object sender, KeyEventArgs ev)
        {
            int? keyid = KeyCodeToKeyid(ev.KeyCode);

            if (keyid != null && isKeyDown.ContainsKey((int)keyid))
            {
                isKeyDown[(int)keyid] = false;  // removeでも良い感
            }
        }

        void form_KeyDown(object sender, KeyEventArgs ev)
        {
            int? keyid = KeyCodeToKeyid(ev.KeyCode);

            if (keyid != null && isKeyDown.GetValueOrDefault((int)keyid) == false)
            {
                isKeyDown[(int)keyid] = true;

                double CSP = player.CurrentSongPosition();

                LastKeyEvent = new KeyEvent
                {
                    keyid = (int)keyid,
                    seconds = CSP
                };
                lock (LastKeyEventDict)
                {
                    LastKeyEventDict[(int)keyid] = CSP;
                }

                lock (KeyEventList)
                {
                    KeyEventList.Enqueue(LastKeyEvent);
                }

                KeyDown(sender, (int)keyid);
            }
        }

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
    }
}
