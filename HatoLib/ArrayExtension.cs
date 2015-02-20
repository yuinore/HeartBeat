using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    static class ArrayExtension
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
    }
}
