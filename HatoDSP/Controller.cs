using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class Controller  // セルに付いているツマミやスイッチを表す
    {
        public string Expression;

        public float Value;

        public readonly string Name;

        public bool ExpressionEnabled
        {
            get { return Expression != null; }
        }

        public Controller(string name, float value)
        {
            Name = name;
            Value = value;
        }
    }
}
