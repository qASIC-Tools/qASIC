using System;
using System.Reflection;

namespace qASIC.Logging;

public abstract class qLogModifier
{
    public virtual bool NeedsCallingType => false;

    public abstract void ModifyLog(qLog log, MethodBase callingMethod, Type callingType);
}
