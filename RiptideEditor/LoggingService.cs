namespace RiptideEditor;

internal sealed class LoggingService : ILoggingService {
    public Logger Logger { get; private set; }

    public LoggingService() {
        Logger = new(32);
        Logger.OnLogAdded += record => {
            var cc = Console.ForegroundColor;

            switch (record.Type) {
                case LoggingType.Info: Console.ForegroundColor = ConsoleColor.White; break;
                case LoggingType.Warning: Console.ForegroundColor = ConsoleColor.Yellow; break;
                case LoggingType.Error: Console.ForegroundColor = ConsoleColor.Red; break;
            }

            Console.WriteLine(record.Message);

            Console.ForegroundColor = cc;
        };
    }

    public void Log(LoggingType type, string? message) {
        Logger.Log(type, message);
    }

    public void Dispose() { }
}