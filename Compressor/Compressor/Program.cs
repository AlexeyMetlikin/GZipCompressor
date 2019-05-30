using System;
using Compressor.Constants;
using Compressor.Extensions;
using Compressor.Models;

namespace Compressor
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length != 3)
                    throw new Exception(ParamsValidationErrorMessages.IncorrectInputParameters);

                var paramsModel = GetParamsModel(args);
                new GZipCompressor().ProcessFileAccordingToCompressionMode(paramsModel);
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine(e.Message);
                return 1;
            }
        }

        private static ParamsModel GetParamsModel(string[] args)
        {
            var paramsModel = new ParamsModel(args);
            var validationResult = paramsModel.ValidateModel();
            if (!validationResult.IsValid)
                throw new Exception(string.Join("\n", validationResult.ErrorMessages));

            return paramsModel;
        }
    }
}