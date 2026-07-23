using NUnit.Framework;
using OSDPBench.Core.Services;

namespace OSDPBench.Core.Tests.Services
{
    [TestFixture(TestOf = typeof(HexConverter))]
    public class HexConverterTests
    {
        private const string ExpectedKey = "00112233445566778899AABBCCDDEEFF";

        [TestCase("00-11-22-33-44-55-66-77-88-99-AA-BB-CC-DD-EE-FF", TestName = "Hyphen delimited")]
        [TestCase("00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF", TestName = "Colon delimited")]
        [TestCase("00 11 22 33 44 55 66 77 88 99 AA BB CC DD EE FF", TestName = "Space delimited")]
        [TestCase("0011-2233-4455-6677-8899-AABB-CCDD-EEFF", TestName = "Two byte groups")]
        [TestCase("  00112233445566778899AABBCCDDEEFF  ", TestName = "Surrounding whitespace")]
        [TestCase("0x00112233445566778899AABBCCDDEEFF", TestName = "Hex prefix")]
        [TestCase("0X00112233445566778899AABBCCDDEEFF", TestName = "Uppercase hex prefix")]
        [TestCase("00112233445566778899aabbccddeeff", TestName = "Lowercase")]
        [TestCase(ExpectedKey, TestName = "Already normalized")]
        public void NormalizeHexInput_DelimitedKey_ReturnsBareUppercaseHex(string input)
        {
            var result = HexConverter.NormalizeHexInput(input);

            Assert.That(result, Is.EqualTo(ExpectedKey));
        }

        [Test]
        public void NormalizeHexInput_HexPrefix_DoesNotShiftRemainingCharacters()
        {
            // Stripping the 'x' as a delimiter would yield "000011..." - a different, silently wrong key
            var result = HexConverter.NormalizeHexInput("0x0011");

            Assert.That(result, Is.EqualTo("0011"));
        }

        [Test]
        public void NormalizeHexInput_DelimitedKey_FitsWithinTheThirtyTwoCharacterLimit()
        {
            var result = HexConverter.NormalizeHexInput("00-11-22-33-44-55-66-77-88-99-AA-BB-CC-DD-EE-FF");

            Assert.That(result, Has.Length.EqualTo(32));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("----")]
        [TestCase("::")]
        [TestCase("xyz")]
        public void NormalizeHexInput_NoHexCharacters_ReturnsEmpty(string input)
        {
            var result = HexConverter.NormalizeHexInput(input);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void NormalizeHexInput_TrailingNewLine_IsRemoved()
        {
            // Copying a key out of a text file or email commonly picks up the line break
            var result = HexConverter.NormalizeHexInput(ExpectedKey + "\r\n");

            Assert.That(result, Is.EqualTo(ExpectedKey));
        }

        [Test]
        public void FromHexString_ValidKey_ReturnsBytes()
        {
            var result = HexConverter.FromHexString(ExpectedKey, 32);

            Assert.That(result, Is.EqualTo(new byte[]
            {
                0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
            }));
        }

        [Test]
        public void FromHexString_NormalizedDelimitedKey_ReturnsBytes()
        {
            var normalized = HexConverter.NormalizeHexInput("00-11-22-33-44-55-66-77-88-99-AA-BB-CC-DD-EE-FF");

            var result = HexConverter.FromHexString(normalized, 32);

            Assert.That(result, Has.Length.EqualTo(16));
        }

        [Test]
        public void FromHexString_WrongLength_Throws()
        {
            Assert.That(() => HexConverter.FromHexString("0011", 32), Throws.ArgumentException);
        }
    }
}
