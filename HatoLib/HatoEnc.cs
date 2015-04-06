using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HatoLib
{
    /// <summary>
    /// 文字のエンコード機能を提供します。
    /// </summary>
    static class HatoEnc
    {
        public static readonly String EncodingName = "Shift_JIS";
        public static readonly Encoding Encoding;

        static HatoEnc()  // 静的コンストラクタ
        {
            // 静的コンストラクタでは例外が起きてほしくない（ような気がする）
            if (Encoding == null)
            {
                Encoding = Encoding.GetEncoding(EncodingName);
            }
        }

        /// <summary>
        /// byte配列をいい感じにstringに変換します。
        /// </summary>
        public static String Encode(byte[] buf)
        {
            if (buf[buf.Length - 1] == (byte)0)
            {
                return Encoding.GetString(buf, 0, buf.Length - 1);  // Acid等の一部のソフトでは、文字列の末尾に'\0'が追加されることがある。
            }
            return Encoding.GetString(buf, 0, buf.Length);
        }

        /// <summary>
        /// stringをbyte配列に変換します。
        /// </summary>
        public static byte[] Encode(String s)
        {
            return Encoding.GetBytes(s);
        }
    }
}
