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
        [TestCase("song.mp3")]
        public void CompressAndDecompressFile_WithExistFile_FilesAreSame(string fileName)
        {
            // Arrange
            var inputFile = FileHelper.GetFullPathByFileName(fileName);
            var outputFile = $"{inputFile}.gz";
            var decompressFile = $"{inputFile}_decompressed{Path.GetExtension(inputFile)}";
            var gZipCompressor = new GZipCompressor();

            // Act
            gZipCompressor.ProcessFileAccordingToCompressionMode(new ParamsModel
            {
                CompressionMode = CompressionMode.Compress,
                InputFileName = inputFile,
                OutputFileName = outputFile
            });

            gZipCompressor.ProcessFileAccordingToCompressionMode(new ParamsModel
            {
                CompressionMode = CompressionMode.Decompress,
                InputFileName = outputFile,
                OutputFileName = decompressFile
            });

            // Assert
            using (var input = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (var decompressed = new FileStream(decompressFile, FileMode.Open, FileAccess.Read))
            {
                Assert.AreEqual(input, decompressed);
            }
        }
    }
}