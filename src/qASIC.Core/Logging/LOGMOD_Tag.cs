using System;
using System.Reflection;

namespace qASIC.Logging
{
    public class LOGMOD_Tag : qLogModifier
    {
        public override bool NeedsCallingType => true;

        public override void ModifyLog(qLog log, MethodBase callingMethod, Type callingType)
        {
            var attr = callingMethod?.GetCustomAttribute<qLogTagAttribute>() ?? callingType?.GetCustomAttribute<qLogTagAttribute>();
            if (attr != null)
                log.tag = attr.Tag;
        }
    }
}