using System;

namespace qASIC
{
    [qSkipLogModifiers]
    public static partial class qDebug
    {
        public const string DEFAULT_TAG = qLog.DEFAULT_TAG;
        public const string WARNING_TAG = "warning";
        public const string ERROR_TAG = "error";
        public const string DEBUG_TAG = "debug";

        public static event Action<qLog> OnLog;

        public static void Log(object message) =>
            OnLog?.Invoke(qLog.CreateNow(message?.ToString() ?? "NULL", DEFAULT_TAG));

        public static void LogWarning(object message) =>
            OnLog?.Invoke(qLog.CreateNow(message?.ToString() ?? "NULL", WARNING_TAG));

        public static void LogError(object message) =>
            OnLog?.Invoke(qLog.CreateNow(message?.ToString() ?? "NULL", ERROR_TAG));

        public static void LogDebug(object message) =>
            OnLog?.Invoke(qLog.CreateNow(message?.ToString() ?? "NULL", DEBUG_TAG));

        public static void Log(object message, string colorTag) =>
            OnLog?.Invoke(qLog.CreateNow(message?.ToString() ?? "NULL", colorTag));

        public static void Log(object message, qColor color) =>
            OnLog?.Invoke(qLog.CreateNow(message?.ToString() ?? "NULL", color));
    }
}
