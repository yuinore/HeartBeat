using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HatoLib
{
    public static class HatoPath
    {
        public static string FromAppDir(string path) {
            return Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), path);
        }
    }
}
