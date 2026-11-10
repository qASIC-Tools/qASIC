namespace qASIC.Logging;

/// <summary>The object contains it's own <see cref="qLogManager"/> and uses it for custom messages.</summary>
public interface IHasLogs
{
    /// <summary>Log manager used by the object.</summary>
    qLogManager Logs { get; }
}
