using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Compressor.Attributes
{
    public class IsFileExistsAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return File.Exists(value?.ToString());
        }
    }
}