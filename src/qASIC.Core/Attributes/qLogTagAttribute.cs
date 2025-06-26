using System;

namespace qASIC
{
    /// <summary>Attribute for adding prefixes to logs.</summary>
    /// <example>If a class that has a [LogPrefix("Settings")] attribute logs "Loaded settings" will show up as "[Settings] Loaded settings" in the console.</example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class qLogTagAttribute : Attribute
    {
        public qLogTagAttribute(string tag)
        {
            Tag = tag;
        }

        public string Tag { get; private set; }
    }
}