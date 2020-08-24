using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnionPeeler.Tomtel;
using System;
using System.Linq;
using System.Text;

namespace OnionPeeler
{
    [TestClass]
    public class TomtelTests
    {
        private readonly string SampleSource = @"
            50 48  # MVI b <- 72
            C2     # ADD a <- b
            02     # OUT a
            A8 4D 00 00 00  # MVI32 ptr <- 0x0000004d
            4F     # MV a <- (ptr+c)
            02     # OUT a
            50 09  # MVI b <- 9
            C4     # XOR a <- b
            02     # OUT a
            02     # OUT a
            E1 01  # APTR 0x00000001
            4F     # MV a <- (ptr+c)
            02     # OUT a
            C1     # CMP
            22 1D 00 00 00  # JNZ 0x0000001d
            48 30  # MVI a <- 48
            02     # OUT a
            58 03  # MVI c <- 3
            4F     # MV a <- (ptr+c)
            02     # OUT a
            B0 29 00 00 00  # MVI32 pc <- 0x00000029
            48 31  # MVI a <- 49
            02     # OUT a
            50 0C  # MVI b <- 12
            C3     # SUB a <- b
            02     # OUT a
            AA     # MV32 ptr <- lb
            57     # MV b <- (ptr+c)
            48 02  # MVI a <- 2
            C1     # CMP
            21 3A 00 00 00  # JEZ 0x0000003a
            48 32  # MVI a <- 50
            02     # OUT a
            48 77  # MVI a <- 119
            02     # OUT a
            48 6F  # MVI a <- 111
            02     # OUT a
            48 72  # MVI a <- 114
            02     # OUT a
            48 6C  # MVI a <- 108
            02     # OUT a
            48 64  # MVI a <- 100
            02     # OUT a
            48 21  # MVI a <- 33
            02     # OUT a
            01     # HALT
            65 6F 33 34 2C  # non-instruction data";

        [TestMethod]
        public void Assembler_Generates_ExpectedByteCode()
        {
            var byteCode = Assembler.Assemble(SampleSource);

            Assert.AreEqual(82, byteCode.Length);

            Assert.AreEqual(0x50, byteCode[0]);
            Assert.AreEqual(0x2c, byteCode[81]);
        }

        [TestMethod]
        public void SampleCode_Outputs_HelloWorld()
        {
            var byteCode = Assembler.Assemble(SampleSource);
            var cpu = new Cpu(byteCode);
            var halted = cpu.Run();
            Assert.IsTrue(halted, "Program did not halt.");

            var expected = Encoding.ASCII.GetBytes("Hello, world!");
            Assert.IsTrue(expected.SequenceEqual(cpu.Output.ToArray()));
        }
    }
}
