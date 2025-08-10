using System;
using System.Collections.Generic;

namespace qASIC
{
    public class qCommandException : Exception
    {
        public qCommandException() : base() { }
        public qCommandException(string message) : base(message) { }

        public override string ToString() =>
            $"{Message}: {StackTrace}";

        public string ToString(bool includeStackTrace) =>
            includeStackTrace ?
                ToString() :
                Message;
    }

    public class qCommandParseException : qCommandException
    {
        public qCommandParseException(Type type, string arg)
        {
            this.type = type;
            this.arg = arg;
        }

        public Type type;
        public string arg;

        public override string Message =>
            $"Unable to parse '{arg}' to {type}";
    }

    public class qCommandArgsCountException : qCommandException
    {
        public qCommandArgsCountException() { }
        public qCommandArgsCountException(int inputArgsCount, int minArgsCount, int maxArgsCount)
        {
            this.inputArgsCount = inputArgsCount;
            this.minArgsCount = minArgsCount;
            this.maxArgsCount = maxArgsCount;
        }

        /// <summary>Type of how the argument acount was missmatched</summary>
        public enum MissmatchType
        {
            /// <summary>When the number of arguments provided by the user is less than the expected amount.</summary>
            NotEnough,
            /// <summary>When the number of arguments provided by the user is more than the excepted amount.</summary>
            TooMany,
            /// <summary>When the number of arguments provided by the user is correct, but the exception was thrown anyway.</summary>
            Invalid,
        }

        public int inputArgsCount;
        public int minArgsCount;
        public int maxArgsCount;

        public MissmatchType Reason
        {
            get
            {
                if (inputArgsCount < minArgsCount)
                    return MissmatchType.NotEnough;

                if (inputArgsCount > maxArgsCount)
                    return MissmatchType.TooMany;

                return MissmatchType.Invalid;
            }
        }

        public override string Message => Reason switch
        {
            MissmatchType.NotEnough => "Not enough arguments",
            MissmatchType.TooMany => "Too many arguments",
            _ => "Invalid argument count",
        };
    }

    public class qCommandOptionException : qCommandException
    {
        public qCommandOptionException() { }
        public qCommandOptionException(string arg, IEnumerable<string> options)
        {
            this.arg = arg;
            this.options = options;
        }

        public IEnumerable<string> options;
        public string arg;

        public override string Message =>
            $"Option '{arg}' does not exist! Avaliable options: {string.Join(", ", options)}";
    }
}