using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Compressor.Attributes;
using Compressor.Helpers;
using NUnit.Framework;

namespace Compressor.Tests.AttributesTests
{
    internal class IsLessThanMaxAllowedFileNameLengthAttributeTests
    {
        [IsLessThanMaxAllowedFileNameLength]
        public string ValidationProperty { get; set; }

        [Test]
        [TestCase(-1, true)]
        [TestCase(0, true)]
        [TestCase(1, false)]
        public void ValidateProperty_WithDifferentValues_GetExpectedResult(int count, bool expectedResult)
        {
            var maxPathLength = PathHelper.GetMaxPathLength() + count;
            if (maxPathLength == count)
                return;

            // Arrange
            ValidationProperty = GetRandomStringByLength(maxPathLength);
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(this) {MemberName = nameof(ValidationProperty)};

            // Act
            var isPropertyValid =
                Validator.TryValidateProperty(ValidationProperty, validationContext, validationResults);

            // Assert
            Assert.AreEqual(expectedResult, isPropertyValid);
        }

        private static string GetRandomStringByLength(int length)
        {
            var randomString = new StringBuilder();
            for (var i = 0; i < length; i++)
                randomString.Append(new Random().Next(0, 9));
            return randomString.ToString();
        }
    }
}