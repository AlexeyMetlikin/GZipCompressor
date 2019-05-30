using Compressor.Extensions;
using NUnit.Framework;

namespace Compressor.Tests.ExtensionsTests
{
    internal class StringExtensionsTests
    {
        [Test]
        [TestCase("132", "132")]
        [TestCase("____", "____")]
        [TestCase(",.!&2123", ",.!&2123")]
        [TestCase("inputString", "InputString")]
        [TestCase("iNPUT_STRING", "INPUT_STRING")]
        [TestCase("INPUT_STRING", "INPUT_STRING")]
        [TestCase(null, null)]
        [TestCase("", "")]
        [TestCase("1", "1")]
        [TestCase(",", ",")]
        [TestCase("a", "A")]
        [TestCase("A", "A")]
        public void TrySetUpperFirstLetter_WithDifferentStrings_GetExpectedResults(
            string inputStr, string expectedResult)
        {
            // Act
            var resultStr = inputStr.WithFirstLetterUpper();

            // Assert
            Assert.AreEqual(expectedResult, resultStr);
        }
    }
}