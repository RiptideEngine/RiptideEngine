namespace RiptideEngine.Core;

public interface ILoggingService : IRiptideService {
    void Log(LoggingType type, string? message);
}