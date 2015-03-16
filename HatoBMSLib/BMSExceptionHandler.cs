using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoBMSLib
{
    /// <summary>
    /// BMSファイルにおけるエラーと警告を扱います。
    /// エラーとは、終端と始点が組になっていないLNや、BPMが定義されていない場合に発生するものです。
    /// エラーが発生した場合は、BMSが適切に再生されなくてもよいものとします。
    /// 警告とは、TOTAL値が指定されていない場合や、PLAYLEVELが文字列指定されている場合に発生するものです。
    /// 動作は未定義となりますが、プログラムが例外により終了することは無いものとします。
    /// （が、できるだけ正しく読み込めるように努めます。）
    /// </summary>
    public class BMSExceptionHandler
    {
        public bool AbortByError = true;
        public bool AbortByWarning = false;

        StringBuilder MessageBuilder = new StringBuilder();

        public string Meesage
        {
            get
            {
                return MessageBuilder.ToString();
            }
        }

        public void ThrowFormatError(string msg)
        {
            lock (MessageBuilder)
            {
                if (AbortByError)
                {
                    throw new FormatException(msg);
                }
                Console.WriteLine("Error: " + msg);
                MessageBuilder.Append("Error: ");
                MessageBuilder.Append(msg);
                MessageBuilder.Append("\n");
            }
        }

        public void ThrowFormatWarning(string msg)
        {
            lock (MessageBuilder)
            {
                if (AbortByWarning)
                {
                    throw new FormatException(msg);
                }
                Console.WriteLine("Warning: " + msg);
                MessageBuilder.Append("Warning: ");
                MessageBuilder.Append(msg);
                MessageBuilder.Append("\n");
            }
        }
    }
}
