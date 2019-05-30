using System.Collections.Generic;

namespace Compressor.Models
{
    public class ValidationModel
    {
        public IEnumerable<string> ErrorMessages { get; set; }

        public bool IsValid { get; set; }
    }
}