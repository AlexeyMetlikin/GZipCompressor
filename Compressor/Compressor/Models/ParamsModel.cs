using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Compressor.Attributes;
using Compressor.Constants;
using Compressor.Extensions;

namespace Compressor.Models
{
    public class ParamsModel
    {
        private const string DefaultCompressionExtension = ".gz";

        public ParamsModel()
        {
        }

        public ParamsModel(IReadOnlyList<string> parameters)
        {
            CompressionMode = GetCompressMode(parameters[0]);
            InputFileName = GetInputFileName(parameters[1]);
            OutputFileName = string.IsNullOrEmpty(Path.GetExtension(parameters[2])) &&
                             CompressionMode == System.IO.Compression.CompressionMode.Compress
                ? parameters[2] + DefaultCompressionExtension
                : parameters[2];
        }

        [Required(ErrorMessage = ParamsValidationErrorMessages.CompressionModeIsRequired)]
        public CompressionMode? CompressionMode { get; set; }

        [Required(ErrorMessage = ParamsValidationErrorMessages.InputFileNameIsRequired)]
        [IsFileExists(ErrorMessage = ParamsValidationErrorMessages.InputFileMustExists)]
        public string InputFileName { get; set; }

        [Required(ErrorMessage = ParamsValidationErrorMessages.OutputFileNameIsRequired)]
        [IsLessThanMaxAllowedFileNameLength(ErrorMessage = ParamsValidationErrorMessages.OutputFileNameIsTooLong)]
        public string OutputFileName { get; set; }

        private static string GetInputFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || !string.IsNullOrEmpty(Path.GetExtension(fileName)))
                return fileName;

            var filesInDirectory = Directory.GetFiles(Path.GetDirectoryName(fileName));
            var filesWithFileName = filesInDirectory.Where(filePath =>
                    string.Compare(Path.GetFileNameWithoutExtension(filePath), Path.GetFileName(fileName),
                        StringComparison.CurrentCultureIgnoreCase) == 0)
                .ToList();
            return filesWithFileName.Count == 1 ? filesWithFileName.First() : null;
        }

        private static CompressionMode? GetCompressMode(string compressionModeString)
        {
            return Enum.TryParse(compressionModeString.WithFirstLetterUpper(), out CompressionMode compressionMode)
                ? compressionMode
                : default(CompressionMode?);
        }
    }
}