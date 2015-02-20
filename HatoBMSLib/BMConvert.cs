using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoBMSLib
{
    public static class BMConvert
    {
        /// <summary>
        /// 長さが２の36進数に変換します
        /// </summary>
        public static String ToBase36(int n)
        {
            int i;
            int slen = 2;
            String s2 = "";
            String num36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (i = 0; i < slen; i++)
            {
                s2 = num36[n % 36] + s2;
                n /= 36;
            }
            // この関数って、BMSPlacementクラスにも同じものがあって、そこだと例外は出さないのね・・・。
            if (n > 0) throw new Exception("数値(#WAV番号)が変換できる範囲(01～ZZ)を超えています。midiのノート数が多すぎるか、red modeです。＠BMSParser.IntToHex36Upper(int n)");
            return s2;
        }

        public static int FromBase36(String s)
        {
            // 昔の私の興味はコードを短くすること、今の私の興味は処理を速くすること、もしかしたらそうなのかもしれない
            int i, n = 0, x;
            String num36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            for (i = 0; i < s.Length; i++)
            {
                x = num36.IndexOf(s[i]);
                if (x < 0) throw new Exception("36進数に用いられない文字が含まれています＠BMSParser.IntFromHex36(String s)");
                n = x % 36 + n * 36;
            }
            return n;
        }

        public static int FromBase16(String s)
        {
            // 昔の私の興味はコードを短くすること、今の私の興味は処理を速くすること、もしかしたらそうなのかもしれない
            int i, n = 0, x;
            String num16 = "0123456789ABCDEF0123456789abcdef";
            for (i = 0; i < s.Length; i++)
            {
                x = num16.IndexOf(s[i]);
                if (x < 0) throw new Exception("16進数に用いられない文字が含まれています＠BMSParser.IntFromHex36(String s)");
                n = x % 16 + n * 16;
            }
            return n;
        }
    }
}
