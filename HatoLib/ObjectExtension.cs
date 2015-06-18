using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoLib
{
    static class ObjectExtension
    {
        public static bool TryCast<T>(this object obj, out T result) where T : class
        {
            return (result = obj as T) != null;
        }
    }
}
