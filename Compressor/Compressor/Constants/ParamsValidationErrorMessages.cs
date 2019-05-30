namespace Compressor.Constants
{
    public class ParamsValidationErrorMessages
    {
        public const string IncorrectInputParameters =
            "Введено неверное количество параметров. Введите параметры в формате: \n" +
            "GZipTest.exe compress/decompress [имя исходного файла] [имя результирующего файла]";

        public const string CompressionModeIsRequired = "Требуеся указать операцию кодирования/декодирования";

        public const string InputFileNameIsRequired = "Требуеся указать имя исходного файла";

        public const string InputFileMustExists = "Требуется указать сущестующий исходный файл";

        public const string OutputFileNameIsRequired = "Требуеся указать имя выходного файла";

        public const string OutputFileNameIsTooLong = "Слишком длинный полный путь выходного файла";
    }
}