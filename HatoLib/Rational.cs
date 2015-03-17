using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoLib
{
    /// <summary>
    /// 有理数の計算をします。
    /// 自動で約分するため、コンストラクタに与えた値とは分子や分母が異なっていることがあります。
    /// また、無限大などの値を持ちません。
    /// 
    /// この型は128ビットと値が大きいため、可能であればfloat型などの他の型を検討して下さい。
    /// 　
    /// この型は、値型の不変型であり、かつ ==, !=, &lt;, &gt; 演算子をオーバーロードします。
    /// 
    /// 整数部分と小数部分を分けたクラスを作ろうかと思ったけどさすがに面倒でやめた。
    /// </summary>
    [Serializable]
    public struct Rational : IComparable<Rational>
    {
        /// <summary>
        /// 分子。
        /// </summary>
        private readonly long numerator;

        /// <summary>
        /// 分母。必ず常に正でなければなりません。
        /// ただし、分子が0の場合は、0であることが許容されています。
        /// また、分子と分母は互いに素でなければなりません。これはGetHashCodeの実装に重要です。
        /// </summary>
        private readonly long denominator;

        public Rational(long nume)
        {
            numerator = nume;
            denominator = 1;
        }

        public Rational(Rational t)
        {
            numerator = t.numerator;
            denominator = t.denominator;
        }

        public Rational(long nume, long deno)
        {
            numerator = nume;
            denominator = deno;
            if (denominator == 0)
            {
                throw new Exception("分母が0です at Frac(int nume, int deno)");
            }
            if (denominator < 0)
            {
                numerator = -nume;
                denominator = -deno;
            }

            // 正規化を行います
            {
                long j;
                long n2 = numerator, d2 = denominator;

                if (numerator == 0)
                {
                    denominator = 1;
                    return;
                }
                if (numerator < 0)
                {
                    n2 = -numerator;
                }
                if (denominator <= 0)
                {
                    throw new Exception("分母が不正です at Frac(int nume, int deno)");
                }

                while ((d2 & 1) == 0 && (n2 & 1) == 0) { numerator >>= 1; denominator >>= 1; n2 >>= 1; d2 >>= 1; }
                while ((n2 & 1) == 0) { n2 >>= 1; }
                while ((d2 & 1) == 0) { d2 >>= 1; }

                j = 3;
                while ((d2 % j) == 0 && (n2 % j) == 0) { numerator /= j; denominator /= j; n2 /= j; d2 /= j; }
                while ((n2 % j) == 0) { n2 /= j; }
                while ((d2 % j) == 0) { d2 /= j; }

                j = 5;
                while ((d2 % j) == 0 && (n2 % j) == 0) { numerator /= j; denominator /= j; n2 /= j; d2 /= j; }
                while ((n2 % j) == 0) { n2 /= j; }
                while ((d2 % j) == 0) { d2 /= j; }

                for (j = d2; j >= 7; j--)
                {
                    while ((d2 % j) == 0 && (n2 % j) == 0) { numerator /= j; denominator /= j; n2 /= j; d2 /= j; }
                }
            }
        }

        public static Rational FromDouble(double real_number)
        {
            long numerator;
            long denominator;
            // なぜRationalを使おうと思ってしまったのか

            // 2^23 = 8388608 (単精度)なのでこれ以下にしたい感はある

            // 15360 == 3 * 5 * 1024
            // 10644480 == 27 * 5 * 7 * 11 * 1024
            // 4838400 = 1024 * 27 * 25 * 7
            if (real_number == Math.Round(real_number))
            {
                return new Rational((long)real_number);
            }
            else
            {
                double seisuu_part = Math.Floor(real_number);
                double shousuu_part = real_number - seisuu_part;

                double gosa1 = 0, gosa2 = 0;
                Rational fr1, fr2;

                {
                    fr1 = new Rational(
                        (long)Math.Round(shousuu_part * 4838400),
                        4838400);
                    gosa1 = Math.Abs((double)fr1 - shousuu_part);
                }
                {
                    fr2 = new Rational(
                        (long)Math.Round(shousuu_part * 1000000),
                        1000000);
                    gosa2 = Math.Abs((double)fr2 - shousuu_part);
                }
                if (gosa1 <= gosa2)
                {
                    numerator = fr1.numerator;
                    denominator = fr1.denominator;
                }
                else
                {
                    numerator = fr2.numerator;
                    denominator = fr2.denominator;
                }

                // 整数部を足します。

                return new Rational(numerator, denominator) + (long)seisuu_part;
            }
        }

        public static implicit operator Rational(long n)
        {
            return new Rational(n);
        }
        public static explicit operator double(Rational fr)
        {
            return fr.numerator / (double)fr.denominator;
        }

        public override String ToString()
        {
            return "(" + numerator + " / " + denominator + ")";
        }

        public static Rational operator +(Rational a, Rational b)
        {
            // 加算する
            if (a.denominator == b.denominator)
            {
                return new Rational(a.numerator + b.numerator, a.denominator);
            }
            else
            {
                return new Rational(
                    a.numerator * b.denominator + a.denominator * b.numerator,
                    a.denominator * b.denominator);
            }
        }

        public static Rational operator -(Rational a, Rational b)
        {
            // 加算する
            if (a.denominator == b.denominator)
            {
                return new Rational(a.numerator - b.numerator, a.denominator);
            }
            else
            {
                return new Rational(
                    a.numerator * b.denominator - a.denominator * b.numerator,
                    a.denominator * b.denominator);
            }
        }

        public static Rational operator *(Rational a, Rational b)
        {
            // 加算する
            return new Rational(
                a.numerator * b.numerator,
                a.denominator * b.denominator);
        }

        /// <summary>
        /// 分母を maxDeno の約数に制限します。
        /// </summary>
        public Rational LimitDenominator(long maxDeno)
        {
            if (maxDeno <= 0)
            {
                throw new Exception("maxDenoが不正な値です at Frac(int nume, int deno)");
            }

            if (maxDeno % denominator != 0)
            {
                return new Rational(
                    (int)Math.Round(numerator * maxDeno / (double)denominator),
                    maxDeno
                );
            }
            return this;
        }

        /// <summary>
        /// 正なら正の整数を、負なら負の整数を、0なら0を返します。
        /// </summary>
        public long Sgn()
        {
            return (denominator > 0) ? 1 : -1;
        }

        public int CompareTo(Rational b)
        {
            long dif = numerator * b.denominator - b.numerator * denominator;
            if (dif > 0) return 1;
            if (dif < 0) return -1;
            return 0;
        }


        /// <summary>
        /// 同じ有理数を表すときに、真を返します。
        /// ＞変更不可能な型以外で演算子 == をオーバーライドすることはお勧めしません。
        /// ＞変更不可能な型以外で演算子 == をオーバーライドすることはお勧めしません。
        /// ＞変更不可能な型以外で演算子 == をオーバーライドすることはお勧めしません。
        /// http://msdn.microsoft.com/ja-jp/library/ms173147(v=vs.90).aspx
        /// </summary>
        public static bool operator ==(Rational a, Rational b)
        {
            return a.numerator * b.denominator == a.denominator * b.numerator;
        }
        /// <summary>
        /// 異なる有理数を表すときに、真を返します。
        /// </summary>
        public static bool operator !=(Rational a, Rational b)
        {
            return a.numerator * b.denominator != a.denominator * b.numerator;
        }
        /// <summary>
        /// オペランドが共にFrac型であり、なおかつ同じ有理数を表すときに、真を返します。
        /// (new Frac(3)).Equals(3) は多分falseを返します(適当
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is Rational)) return false;
            Rational b = (Rational)obj;
            //if ((object)b == null) return false; // これが無いのは重大なバグだった可能性？いや、そうでもないか？
            return this.numerator * b.denominator == this.denominator * b.numerator;
        }
        public override int GetHashCode()
        {
            //return (int)(n * 932187.0 / d);  // これってバグらないですか？(精度的な意味で
            return (int)(numerator ^ denominator);  // オーバーフローしたらバグるかも！！！！
        }

        public static bool operator >(Rational a, Rational b)
        {
            if (a.denominator == b.denominator)
            {
                return a.numerator > b.numerator;
            }
            else
            {
                return a.numerator * b.denominator > b.numerator * a.denominator;
            }
        }

        public static bool operator <(Rational a, Rational b)
        {
            if (a.denominator == b.denominator)
            {
                return a.numerator < b.numerator;
            }
            else
            {
                return a.numerator * b.denominator < b.numerator * a.denominator;
            }
        }

    }
}
