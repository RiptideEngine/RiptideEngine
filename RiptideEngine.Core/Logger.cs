using RiptideEngine.Core.Attributes;

namespace RiptideEngine.Core {
    [EnumExtension]
    public enum LoggingType {
        Info,
        Warning,
        Error,
    }

    public sealed class Logger {
        public event Action<LogRecord>? OnLogAdded;
        public event Action<LogRecord>? OnLogDeducted;

        private readonly Queue<LogRecord> _record = new();
        private int _recordCapacity;

        public int RecordCapacity {
            get => _recordCapacity;
            set {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, nameof(value));
                _record.EnsureCapacity(value);

                _recordCapacity = value;
            }
        }

        public int RecordAmount => _record.Count;

        public Logger(int recordCapacity = 32) {
            _record = new(recordCapacity);
            _recordCapacity = recordCapacity;
        }

        public void Log(LoggingType type, string? message) {
            if (_record.Count >= _recordCapacity) {
                OnLogDeducted?.Invoke(_record.Dequeue());
            }

            _record.Enqueue(new LogRecord(type, message));
            OnLogAdded?.Invoke(new(type, message));
        }

        public IEnumerable<LogRecord> EnumerateRecords() => _record;
        public void ClearRecord() => _record.Clear();
    }

    public readonly record struct LogRecord(LoggingType Type, string? Message);
}
