using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using Compressor.Constants;
using Compressor.Extensions;
using Compressor.Models;
using Compressor.Tests.Helpers;
using NUnit.Framework;

namespace Compressor.Tests.ExtensionsTests
{
    internal class ParamsModelValidationExtensionsTests
    {
        private const string NameOfExistFile = "Song.mp3";
        private const string CorrectInputFileName = "InputFileName";
        private const string CorrectOutputFileName = "OutputFileName";
        private const string TooLongOutputFileName =
            "Internet Information Services (IIS) turns a computer into a Web server " +
            "that can provide World Wide Web publishing services, File Transfer Protocol (FTP) " +
            "services, Simple Mail Transport Protocol (SMTP) services, and Network News Transfer " +
            "Protocol (NNTP) services. You can use IIS to host and manage Web sites and other Internet " +
            "content once you obtain an IP address, register your domain on a DNS server, " +
            "and configure your network appropriately. " +
            "IIS is a component of the Microsoft? Windows? operating system.";

        public static IEnumerable<TestCaseData> ParamsModelValidationData
        {
            get
            {
                yield return new TestCaseData(
                        new ParamsModel(),
                        new List<string>
                        {
                            ParamsValidationErrorMessages.CompressionModeIsRequired,
                            ParamsValidationErrorMessages.InputFileNameIsRequired,
                            ParamsValidationErrorMessages.OutputFileNameIsRequired
                        })
                    .SetName("Validation with empty instance of ParamsModel");
                yield return new TestCaseData(
                        new ParamsModel {CompressionMode = CompressionMode.Compress},
                        new List<string>
                        {
                            ParamsValidationErrorMessages.InputFileNameIsRequired,
                            ParamsValidationErrorMessages.OutputFileNameIsRequired
                        })
                    .SetName("Validation with correct CompressionMode (Compress Mode)");
                yield return new TestCaseData(
                        new ParamsModel {CompressionMode = CompressionMode.Decompress}, 
                        new List<string>
                        {
                            ParamsValidationErrorMessages.InputFileNameIsRequired,
                            ParamsValidationErrorMessages.OutputFileNameIsRequired
                        })
                    .SetName("Validation with correct CompressionMode (Decompress Mode)");
                yield return new TestCaseData(
                        new ParamsModel {OutputFileName = CorrectOutputFileName},
                        new List<string>
                        {
                            ParamsValidationErrorMessages.CompressionModeIsRequired,
                            ParamsValidationErrorMessages.InputFileNameIsRequired
                        })
                    .SetName("Validation with filled OutputFileName");
                yield return new TestCaseData(
                        new ParamsModel {InputFileName = CorrectInputFileName},
                        new List<string>
                        {
                            ParamsValidationErrorMessages.CompressionModeIsRequired,
                            ParamsValidationErrorMessages.InputFileMustExists,
                            ParamsValidationErrorMessages.OutputFileNameIsRequired
                        })
                    .SetName("Validation with filled InputFileName, but input file doesn't exist");
                yield return new TestCaseData(
                        new ParamsModel {InputFileName = FileHelper.GetFullPathByFileName(NameOfExistFile)},
                        new List<string>
                        {
                            ParamsValidationErrorMessages.CompressionModeIsRequired,
                            ParamsValidationErrorMessages.OutputFileNameIsRequired
                        })
                    .SetName("Validation with filled and exists InputFileName");
                yield return new TestCaseData(
                        new ParamsModel { OutputFileName = TooLongOutputFileName },
                        new List<string>
                        {
                            ParamsValidationErrorMessages.CompressionModeIsRequired,
                            ParamsValidationErrorMessages.InputFileNameIsRequired,
                            ParamsValidationErrorMessages.OutputFileNameIsTooLong
                        })
                    .SetName("Validation with too long OutputFileName");
            }
        }

        [Test]
        [TestCaseSource(nameof(ParamsModelValidationData))]
        public void ValidateParamsModel_WithDifferentFilledProperties_GetExpectedValidationResults(
            ParamsModel paramsModel, List<string> expectedValidationErrors)
        {
            // Act
            var validationResult = paramsModel.ValidateModel();

            // Assert
            Assert.IsFalse(validationResult.IsValid);

            Assert.AreEqual(expectedValidationErrors.Count, validationResult.ErrorMessages.Count());
            foreach (var expectedValidationError in expectedValidationErrors)
                Assert.IsTrue(validationResult.ErrorMessages
                    .Any(errorMessage => errorMessage.Equals(expectedValidationError)));
        }
    }
}