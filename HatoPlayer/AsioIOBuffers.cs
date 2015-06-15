using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoPlayer
{
    public struct AsioIOBuffers
    {
        public readonly AsioBuffer Input;
        public readonly AsioBuffer Output;

        public AsioIOBuffers(AsioBuffer input, AsioBuffer output)
        {
            Input = input;
            Output = output;
        }
    }
}
