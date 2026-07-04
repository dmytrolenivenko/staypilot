
namespace AddProperty
{
    public static class Logger
    {
        public static void LogInformation(string message)
        {
            Console.WriteLine($"INFO: {message}");
        }
        public static void LogError(string message)
        {
            Console.WriteLine($"ERROR: {message}");
        }
        public static void ToFile(this string message, string filePath)
        {
            File.AppendAllText(filePath, $"{DateTime.UtcNow}: {message}{Environment.NewLine}");
        }
    }
}
