using System.ComponentModel.DataAnnotations;
using Compressor.Helpers;

namespace Compressor.Attributes
{
    public class IsLessThanMaxAllowedFileNameLengthAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            var maxPathLength = PathHelper.GetMaxPathLength();
            return maxPathLength == 0 || !(value?.ToString().Length > maxPathLength);
        }
    }
}