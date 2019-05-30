using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Compressor.Models;

namespace Compressor.Extensions
{
    public static class ParamsModelValidationExtension
    {
        public static ValidationModel ValidateModel(this ParamsModel paramsModel)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(paramsModel);
            if (!Validator.TryValidateObject(paramsModel, validationContext, validationResults, true))
                return new ValidationModel
                {
                    IsValid = false,
                    ErrorMessages = validationResults.Select(validationResult => validationResult.ErrorMessage)
                };

            return new ValidationModel {IsValid = true};
        }
    }
}