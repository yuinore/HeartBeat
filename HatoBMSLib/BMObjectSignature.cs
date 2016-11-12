using HatoLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoBMSLib
{
    public class BMObjectSignature : BMObject
    {
        const int BMSCH_SIGNATURE = 2;

        public double Signature;

        public BMObjectSignature(double signature, Rational measure) :
            base(BMSCH_SIGNATURE, 0, 0, measure)
        {
            this.Signature = signature;
        }
    }
}
