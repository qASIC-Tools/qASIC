using System;
using System.Reflection;

namespace qASIC.Logging;

public class Logmod_Prefix : qLogModifier
{
    public override bool NeedsCallingType => true;

    public override void ModifyLog(qLog log, MethodBase callingMethod, Type callingType)
    {
        var attr = callingMethod?.GetCustomAttribute<qLogPrefixAttribute>() ?? callingType?.GetCustomAttribute<qLogPrefixAttribute>();
        if (attr != null && attr.ValidPrefix)
            log.message = attr.FormatMessage(log.message);
    }
}
