using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HatoDSP;
using HatoLib;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

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
            float[][] filt = new float[2][];

            iir1.Take(5, new[] { new[] { filt[0] = sig1.ToArray(), filt[1] = sig2.ToArray() } });
            Assert.IsTrue(Signal.Equals(new ExactSignal(filt[0]), new ExactSignal(new float[] { 0, 1, 1, 1, 1 })));
            Assert.IsTrue(Signal.Equals(new ExactSignal(filt[1]), new ExactSignal(new float[] { 5, 1, 1, 1, 1 })));

            iir1.Take(5, new[] { new[] { filt[0] = sig1.ToArray(), filt[1] = sig2.ToArray() } });
            Assert.IsTrue(Signal.Equals(new ExactSignal(filt[0]), new ExactSignal(new float[] { -4, 1, 1, 1, 1 })));
            Assert.IsTrue(Signal.Equals(new ExactSignal(filt[1]), new ExactSignal(new float[] { -4, 1, 1, 1, 1 })));

            var iir2 = new IIRFilter(2, 1, -1, 0, 1, 0, 1);

            iir2.Take(5, new[] { new[] { filt[0] = sig1.ToArray(), filt[1] = sig2.ToArray() } });
            Assert.IsTrue(Signal.Equals(new ExactSignal(filt[0]), new ExactSignal(new float[] { 0, 1, 3, 7, 13 })));
            Assert.IsTrue(Signal.Equals(new ExactSignal(filt[1]), new ExactSignal(new float[] { 5, 11, 23, 37, 53 })));

            LocalEnvironment lenv = new LocalEnvironment() {
                Freq = new ConstantSignal(0, 256),
                Gate = new ConstantSignal(1, 256),
                Pitch = new ConstantSignal(60, 256),
                SamplingRate = 44100,
                Locals = new System.Collections.Generic.Dictionary<string,Signal>(),
            };

            Assembly asm = Assembly.LoadFrom("HatoDSP.dll");

            Type[] types = asm.GetTypes();

            foreach (Type t in types)
            {
                if (t.IsSubclassOf(typeof(Cell)))
                {
                    // Debug.Assert ではなく Assert.IsTrue を使うこと

                    //Console.WriteLine(t.Name);
                    // t は HatoDSP.Cell を継承し、 Cell それ自身ではないクラス。

                    Cell cell = null;
                    CellTree child1 = new CellTree(() => new AnalogOscillator());
                    float[][] sig = null;
                    float[][] sigB = null;
                    int chCnt = 1;

                    cell = (Cell)Activator.CreateInstance(t);  // internalでも構わずインスタンス生成するんですね・・・
                    Assert.IsTrue(cell.ParamsList != null);
                    chCnt = cell.ChannelCount;
                    lenv.Buffer = sig = new float[chCnt][].Select(x => new float[256]).ToArray();
                    cell.Take(256, lenv);  // childの指定なしで実行
                    //Assert.IsTrue(sig.Length >= 1);
                    //for (int ch = 0; ch < sig.Length; ch++) { Assert.IsTrue(sig[ch] == 256); };  // 要素数がTakeで指定した個数と等しいことを確認
                    //Assert.IsTrue(sig.Length == cell.ChannelCount);  // チャンネル数が表明通りであることを確認

                    cell = (Cell)Activator.CreateInstance(t);
                    cell.AssignChildren(new CellWire[] { });
                    chCnt = cell.ChannelCount;
                    lenv.Buffer = sig = new float[chCnt][].Select(x => new float[256]).ToArray();
                    cell.Take(256, lenv);  // childの個数0個で実行
                    //Assert.IsTrue(sig.Length >= 1);
                    //for (int ch = 0; ch < sig.Length; ch++) { Assert.IsTrue(sig[ch].Count == 256); };
                    //Assert.IsTrue(sig.Length == cell.ChannelCount);

                    cell = (Cell)Activator.CreateInstance(t);
                    cell.AssignChildren(new CellWire[] { new CellWire(child1, 0) });
                    chCnt = cell.ChannelCount;
                    lenv.Buffer = sig = new float[chCnt][].Select(x => Enumerable.Range(0, 256).Select(y => (float)(y)).ToArray()).ToArray();
                    cell.Take(256, lenv);  // childの個数1個(ポート0)で実行

                    cell = (Cell)Activator.CreateInstance(t);
                    cell.AssignChildren(new CellWire[] { new CellWire(child1, 0) });
                    chCnt = cell.ChannelCount;
                    lenv.Buffer = sigB /*ここが上と違う*/ = new float[chCnt][].Select(x => new float[256]).ToArray();
                    cell.Take(256, lenv);

                    for (int i = 0; i < 256; i++)  // 以前のバッファが正しく残されているかどうかのテスト
                    {
                        Assert.IsTrue(Math.Abs(sig[0][i] - (sigB[0][i] + i)) <= 0.000);  // 誤差 -100dB 以下
                    }
                }
            }
        }
    }
}
