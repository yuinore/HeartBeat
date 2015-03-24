using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HatoDSP;
using HatoLib;
using System.Linq;

namespace HatoDSPTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            //Assert.IsTrue
            var sig1 = new ExactSignal(new float[] { 0, 1, 2, 3, 4 });
            var sig2 = new ExactSignal(new float[] { 5, 6, 7, 8, 9 });

            var sum = Signal.Add(sig1, sig2);
            var mul = Signal.Multiply(sig1, sig2);

            Assert.IsTrue(Signal.Equals(sum, new ExactSignal(new float[] { 5, 7, 9, 11, 13 })));
            Assert.IsTrue(Signal.Equals(mul, new ExactSignal(new float[] { 0, 6, 14, 24, 36 })));

            var sig3 = new ConstantSignal(3.0f, 5);

            sum = Signal.Add(sig1, sig3);
            mul = Signal.Multiply(sig1, sig3);

            Assert.IsTrue(Signal.Equals(sum, new ExactSignal(new float[] { 3, 4, 5, 6, 7 })));
            Assert.IsTrue(Signal.Equals(mul, new ExactSignal(new float[] { 0, 3, 6, 9, 12 })));

            sum = Signal.Add(sig3, sig3);
            mul = Signal.Multiply(sig3, sig3);

            Assert.IsTrue(Signal.Equals(sum, new ExactSignal(new float[] { 6, 6, 6, 6, 6 })));
            Assert.IsTrue(Signal.Equals(mul, new ExactSignal(new float[] { 9, 9, 9, 9, 9 })));

            var sig4 = new JoinedSignal(new Signal[] {
                new ExactSignal(new float[] {0, 1, 2, 3}),
                new ConstantSignal(4, 1)});

            sum = Signal.Add(sig2, sig4);
            mul = Signal.Multiply(sig2, sig4);

            Assert.IsTrue(Signal.Equals(sum, new ExactSignal(new float[] { 5, 7, 9, 11, 13 })));
            Assert.IsTrue(Signal.Equals(mul, new ExactSignal(new float[] { 0, 6, 14, 24, 36 })));

            var iir1 = new IIRFilter(2, 1, 0, 0, 1, -1, 0);

            var filt = iir1.Take(5, new[] { sig1, sig2 });
            Assert.IsTrue(Signal.Equals(filt[0], new ExactSignal(new float[] { 0, 1, 1, 1, 1 })));
            Assert.IsTrue(Signal.Equals(filt[1], new ExactSignal(new float[] { 5, 1, 1, 1, 1 })));

            filt = iir1.Take(5, new[] { sig1, sig2 });
            Assert.IsTrue(Signal.Equals(filt[0], new ExactSignal(new float[] { -4, 1, 1, 1, 1 })));
            Assert.IsTrue(Signal.Equals(filt[1], new ExactSignal(new float[] { -4, 1, 1, 1, 1 })));

            var iir2 = new IIRFilter(2, 1, 1, 0, 1, 0, 1);

            filt = iir2.Take(5, new[] { sig1, sig2 });
            Assert.IsTrue(Signal.Equals(filt[0], new ExactSignal(new float[] { 0, 1, 3, 7, 13 })));
            Assert.IsTrue(Signal.Equals(filt[1], new ExactSignal(new float[] { 5, 11, 23, 37, 53 })));

            var rainbow = new Rainbow();
            rainbow.AssignChildren(new CellTree(() => new AnalogOscillator()));
            var sig5 = rainbow.Take(100000, new LocalEnvironment
            {
                Freq = new ConstantSignal(441, 100000),
                Pitch = new ConstantSignal(60, 100000),
                Locals = null
            });

            WaveFileWriter.WriteAllSamples(HatoPath.FromAppDir("test1.wav"), sig5.Select(x => x.ToArray()).ToArray());
        }
    }
}
