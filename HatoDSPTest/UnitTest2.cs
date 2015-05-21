using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HatoDSP;

namespace HatoDSPTest
{
    [TestClass]
    public class UnitTest2
    {
        [TestMethod]
        public void TestMethod2()
        {
            Assert.IsTrue(PatchReader.RemoveComments("abc/*DEF*/") == "abc\r\n");
            Assert.IsTrue(PatchReader.RemoveComments("abc//DEF") == "abc\r\n");
            Assert.IsTrue(PatchReader.RemoveComments("/*DEF*/") == "\r\n");
            Assert.IsTrue(PatchReader.RemoveComments("//DEF") == "\r\n");
            Assert.IsTrue(PatchReader.RemoveComments("abc/*DEF*/ghi") == "abc\r\nghi");
            Assert.IsTrue(PatchReader.RemoveComments("abc//DEF\r\nghi") == "abc\r\nghi");

            Assert.IsTrue(PatchReader.RemoveComments(
                @"/*" + "\r\n" +
                @"AA" + "\r\n" +
                @"/*/" + "\r\n" +
                @"bb" + "\r\n" +
                @"//*/" + "\r\n" +
                @"cc" + "\r\n" +
                @"dd//DD" + "\r\n" +
                @"e/*E*/e" + "\r\n" +
                @"/*F" + "\r\n" +
                @"//F*/" + "\r\n" +
                @"f*/" + "\r\n" +
                @"g*g/g*g" + "\r\n" +
                @"h""/*""h""*/""h" + "\r\n" +
                @"i/*I/*I//*I*/i" + "\r\n" +
                @"""\"""" /* JJJ */ ""j\""j""") ==

                @"" + "\r\n" +
                @"" + "\r\n" +
                @"bb" + "\r\n" +
                @"" + "\r\n" +
                @"cc" + "\r\n" +
                @"dd" + "\r\n" +
                @"e" + "\r\n" +
                @"e" + "\r\n" +
                @"" + "\r\n" +
                @"" + "\r\n" +
                @"f*/" + "\r\n" +
                @"g*g/g*g" + "\r\n" +
                @"h""/*""h""*/""h" + "\r\n" +
                @"i" + "\r\n" +
                @"i" + "\r\n" +
                @"""\"""" " + "\r\n" +
                @" ""j\""j""");
        }
    }
}
