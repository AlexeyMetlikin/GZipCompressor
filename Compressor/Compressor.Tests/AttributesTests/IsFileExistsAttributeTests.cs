using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Compressor.Attributes;
using Compressor.Tests.Helpers;
using NUnit.Framework;

namespace Compressor.Tests.AttributesTests
{
    internal class IsFileExistsAttributeTests
    {
        [IsFileExists]
        public string ValidationProperty { get; set; }

        [Test]
        [TestCase("132", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        [TestCase("Song.mp3", true)]
        public void ValidateProperty_WithDifferentValues_GetExpectedResult(string inputStr, bool expectedResult)
        {
            // Arrange
            ValidationProperty = FileHelper.GetFullPathByFileName(inputStr);
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(this) {MemberName = nameof(ValidationProperty)};

            // Act
            var isPropertyValid = Validator.TryValidateProperty(ValidationProperty, validationContext, validationResults);

            // Assert
            Assert.AreEqual(expectedResult, isPropertyValid);
        }
    }
}