using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HatoLib
{
    public class LazyMatch
    {
        string text;
        string pattern;
        RegexOptions options;
        
        public LazyMatch(string text, string pattern, RegexOptions options = RegexOptions.None)
        {
            this.text = text;
            this.pattern = pattern;
            this.options = options;
        }

        public bool Evaluate(out Match match)
        {
            match = Regex.Match(text, pattern, options);
            return match.Success;
        }

    }
}
