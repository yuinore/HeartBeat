using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeartBeatCore
{
    /// <summary>
    /// キー入力イベントを表します。keyidは0～71の数値です。
    /// </summary>
    class KeyEvent
    {
        public int keyid;
        public double seconds;
    }
}
