using System;
using System.Collections.Generic;

namespace qASIC.CmdAutocomplete
{
    public class ACVariant
    {
        public ACVariant() : this(null) { }
        public ACVariant(ACData data) 
        {
            Data = data;
        }

        public ACData Data { get; set; }

        public List<ACArgument> Arguments { get; private set; } = new List<ACArgument>();

        public ACVariant AddArgument(ACArgument parameter)
        {
            Arguments.Add(parameter);
            return this;
        }

        public ACVariant AddType<T>(string name) =>
            AddType(typeof(T), name);

        public ACVariant AddType(Type type, string name)
        {
            if (type.IsAssignableTo(typeof(Enum)))
            {
                Arguments.Add(new ACOptionArgument(type, name, (string[])Enum.GetValues(type)));
                return this;
            }

            Arguments.Add(new ACArgument(type, name));
            return this;
        }

        public ACVariant AddOptions<T>(string name, params string[] options) =>
            AddOptions(typeof(T), name, options);

        public ACVariant AddOptions(Type type, string name, params string[] options)
        {
            Arguments.Add(new ACOptionArgument(type, name, options));
            return this;
        }

        public ACData Finish() =>
            Data;
    }
}
