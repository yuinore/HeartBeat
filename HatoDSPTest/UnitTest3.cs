using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HatoLib;
using System.IO;
using HatoDSP;

namespace HatoDSPTest
{
    [TestClass]
    public class UnitTest3
    {
        [TestMethod]
        public void TestMethod3()
        {
            BitPacker bp = new BitPacker();

            bp.AddInteger(10, 123);
            bp.AddLinkedInteger(8, 123454321);
            bp.AddFloat(12.3456f);
            bp.AddString("Hello World! Chorus/Flanger みょん♪");

            var ret = bp.ToFloatList();

            BinaryWriter w = new BinaryWriter(new FileStream("floatlist.bin", FileMode.Create, FileAccess.Write));
            foreach (var f in ret)
            {
                w.Write(f);
            }
            w.Close();

            BitUnpacker bup = new BitUnpacker(ret);

            StreamWriter w2 = new StreamWriter(new FileStream("unpacked.txt", FileMode.Create, FileAccess.Write));
            {
                var data1 = bup.ReadInteger(10);
                w2.WriteLine(data1);
                Assert.IsTrue(data1 == 123);

                var data2 = bup.ReadLinkedInteger(8);
                w2.WriteLine(data2);
                Assert.IsTrue(data2 == 123454321);

                var data3 = bup.ReadFloat();
                w2.WriteLine(data3);
                Assert.IsTrue(data3 == 12.3456f);

                var data4 = bup.ReadString();
                w2.WriteLine(data4);
                Assert.IsTrue(data4 == "Hello World! Chorus/Flanger みょん♪");
            }
            w2.Close();

            var packed = PatchPacker.Pack(System.IO.File.ReadAllText("patch.txt"));

            w = new BinaryWriter(new FileStream("patchfloatlist.bin", FileMode.Create, FileAccess.Write));
            foreach (var f in packed.ToFloatList())
            {
                w.Write(f);
            }
            w.Close();

            File.WriteAllText("patchpacked.txt", packed.ToString());

            File.WriteAllText("recovered.txt", PatchPacker.Unpack(packed.ToFloatList()));
        }
    }
}
