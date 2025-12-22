using System;
using System.Reflection;

namespace qASIC.Logging;

public class Logmod_Color : qLogModifier
{
    public override bool NeedsCallingType => true;

    public override void ModifyLog(qLog log, MethodBase callingMethod, Type callingType)
    {
        var attr = callingMethod?.GetCustomAttribute<qLogColorAttribute>() ?? callingType?.GetCustomAttribute<qLogColorAttribute>();
        if (attr != null)
        {
            log.color = attr.Color;
        }
    }
}
