using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoBMSLib
{
    /// <summary>
    /// キー入力イベントを表します。keyidは0～71の数値です。
    /// </summary>
    public class KeyEvent
    {
        public bool IsKeyUp;
        public int keyid;
        public double seconds;
    }
}
