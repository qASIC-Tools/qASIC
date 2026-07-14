using System.Collections.Generic;

namespace qASIC.Options;

/// <summary>Used for defining custom options.</summary>
public interface IUsesCustomOptions
{
    /// <summary>Collection of custom options used by the object.</summary>
    IEnumerable<IOption> CustomOptions { get; }
}
