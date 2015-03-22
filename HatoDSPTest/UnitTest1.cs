using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HatoDSP;

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
        }
    }
}
