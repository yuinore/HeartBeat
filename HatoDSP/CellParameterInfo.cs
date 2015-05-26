using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    /// <summary>
    /// セルに付いているツマミやスイッチの実装を表す。
    /// </summary>
    public class CellParameterInfo
    {
        // TODO: オシレーターの選択やトグルスイッチのような、整数値から選択するコントロールに対応

        /// <summary>
        /// このコントロールによって指定できる値の名称
        /// </summary>
        public string Name;

        /// <summary>
        /// エクスプレッションが使用可能かどうかを示す。
        /// </summary>
        public bool ExpressionAvailable;

        /// <summary>
        /// 指定可能な最小値
        /// </summary>
        public float MinValue;

        /// <summary>
        /// 指定可能な最大値
        /// </summary>
        public float MaxValue;

        /// <summary>
        /// GUIで使用される初期値
        /// </summary>
        public float DefaultValue;

        /// <summary>
        /// GUIで使用される単位付きの値
        /// </summary>
        public Func<float, string> Label;

        public CellParameterInfo(string name, bool expr, float min, float max, float def, Func<float, string> label)
        {
            Name = name;
            ExpressionAvailable = expr;
            MinValue = min;
            MaxValue = max;
            DefaultValue = def;
            Label = label;
        }

        public static Func<float, string> IdLabel = x => Math.Round(x * 100) * 0.01 + "";
        public static Func<float, string> PercentLabel = x => Math.Round(x * 10000) * 0.01 + "%";
    }
}
