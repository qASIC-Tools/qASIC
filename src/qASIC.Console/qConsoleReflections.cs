using System.Reflection;
using System.Collections.Generic;
using System;
using System.Linq;

namespace qASIC.Console
{
    internal static class qConsoleReflections
    {
        private static Dictionary<string, qLogColorAttribute> _colorAttributeMethods = null;
        public static Dictionary<string, qLogColorAttribute> ColorAttributeMethods
        {
            get
            {
                if (_colorAttributeMethods == null)
                    _colorAttributeMethods = TypeFinder.FindMethodsWithAttribute<qLogColorAttribute>(FLAGS)
                        .ToDictionary(x => CreateMethodId(x), x => x.GetCustomAttribute<qLogColorAttribute>()); ;

                return _colorAttributeMethods;
            }
        }

        private static Dictionary<string, qLogColorAttribute> _colorAttributeDeclaringTypes = null;
        public static Dictionary<string, qLogColorAttribute> ColorAttributeDeclaringTypes
        { 
            get
            {
                if (_colorAttributeDeclaringTypes == null)
                    _colorAttributeDeclaringTypes = TypeFinder.FindClassesWithAttribute<qLogColorAttribute>(FLAGS)
                        .ToDictionary(x => CreateTypeId(x), x => x.GetCustomAttribute<qLogColorAttribute>());

                return _colorAttributeDeclaringTypes;
            }
        }

        private static Dictionary<string, qLogPrefixAttribute> _prefixAttributeMethods = null;
        public static Dictionary<string, qLogPrefixAttribute> PrefixAttributeMethods 
        { 
            get
            {
                if (_prefixAttributeMethods == null)
                    _prefixAttributeMethods = TypeFinder.FindMethodsWithAttribute<qLogPrefixAttribute>(FLAGS)
                        .ToDictionary(x => CreateMethodId(x), x => x.GetCustomAttribute<qLogPrefixAttribute>());

                return _prefixAttributeMethods;
            }
        }

        private static Dictionary<string, qLogPrefixAttribute> _prefixAttributeDeclaringTypes = null;
        public static Dictionary<string, qLogPrefixAttribute> PrefixAttributeDeclaringTypes 
        { 
            get
            {
                if (_prefixAttributeDeclaringTypes == null)
                    _prefixAttributeDeclaringTypes = TypeFinder.FindClassesWithAttribute<qLogPrefixAttribute>(FLAGS)
                        .ToDictionary(x => CreateTypeId(x), x => x.GetCustomAttribute<qLogPrefixAttribute>());

                return _prefixAttributeDeclaringTypes;
            }
        }

        public static void Initialize()
        {
            _ = ColorAttributeMethods;
            _ = ColorAttributeDeclaringTypes;
            _ = PrefixAttributeMethods;
            _ = PrefixAttributeDeclaringTypes;
        }

        const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static string CreateMethodId(MethodBase method) =>
            method != null ?
            $"{method.DeclaringType?.FullName}/{method}" :
            string.Empty;

        public static string CreateTypeId(Type type) =>
            type.FullName ?? string.Empty;
    }
}