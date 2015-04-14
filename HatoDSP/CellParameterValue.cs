using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    /// <summary>
    /// セルに付いているツマミやスイッチに指定されている値を表す
    /// </summary>
    public class CellParameterValue
    {
        public string Expression;

        public float Value;

        public bool ExpressionEnabled
        {
            get { return Expression != null; }
        }

        public CellParameterValue(float value)
        {
            Value = value;
            Expression = null;
        }
    }
}
