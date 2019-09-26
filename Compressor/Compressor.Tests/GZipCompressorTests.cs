using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Compressor.Models;
using Compressor.Tests.Helpers;
using NUnit.Framework;

namespace Compressor.Tests
{
    public class GZipCompressorTests
    {
        [Test]
        [TestCase(@"C:\beeline_dpc_web_stage_2019-09-26.bak")]
        public void CompressAndDecompressFile_WithExistFile_FilesAreSame(string fileName)
        {
            // Arrange
            var inputFile = FileHelper.GetFullPathByFileName(fileName);
            var outputFile = @"C:\Users\aleksei_metlikin\Desktop\beeline_dpc_web_stage_2019-09-23.gz";
            var decompressFile = $@"C:\Users\aleksei_metlikin\Desktop\beeline_dpc_web_stage_2019_decompressed{Path.GetExtension(inputFile)}";

            // Act
            var s = new Stopwatch();
            s.Start();
            new GZipCompressor().ProcessFileAccordingToCompressionMode(new ParamsModel
            {
                CompressionMode = CompressionMode.Compress,
                InputFileName = inputFile,
                OutputFileName = outputFile
            });
            s.Stop();
            var d = s.ElapsedMilliseconds;

            s.Reset();
            s.Start();
            new GZipCompressor().ProcessFileAccordingToCompressionMode(new ParamsModel
            {
                CompressionMode = CompressionMode.Decompress,
                InputFileName = outputFile,
                OutputFileName = decompressFile
            });
            s.Stop();
            d = s.ElapsedMilliseconds;

            // Assert
            using (var input = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (var decompressed = new FileStream(decompressFile, FileMode.Open, FileAccess.Read))
            {
                Assert.AreEqual(input, decompressed);
            }
        }
    }
}