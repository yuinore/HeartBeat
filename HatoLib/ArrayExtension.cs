using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HatoLib
{
    public static class ArrayExtension
    {
        // http://stackoverflow.com/questions/406485/array-slices-in-c-sharp
        // Skip(int).Take(int) より速いゾ～これ
        public static T[] Slice<T>(this T[] arr, int indexFrom, int indexTo)
        {
            if (indexFrom > indexTo)
            {
                throw new ArgumentOutOfRangeException("indexFrom is bigger than indexTo!");
            }

            int length = indexTo - indexFrom;
            T[] result = new T[length];
            Array.Copy(arr, indexFrom, result, 0, length);

            return result;
        }

        public static double ElementAt(this double[] arr, double pos)
        {
            if (pos < -0.5 || pos > arr.Length - 0.5)
            {
                throw new IndexOutOfRangeException("ていうかもう寝よう。");
            }

            int intpart = (int)Math.Floor(pos);
            double decpart = pos - intpart;

            if (intpart <= -1)
            {
                return arr[0];
            }
            else if (intpart >= arr.Length - 1)
            {
                return arr[arr.Length - 1];
            }

            return (1 - decpart) * arr[intpart] + decpart * arr[intpart + 1];
        }
    }
}
