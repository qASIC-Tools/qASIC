using System;
using System.Collections.Generic;

namespace qASIC
{
    public class qCommandException : Exception
    {
        public qCommandException() : base() { }
        public qCommandException(string message) : base(message) { }

        public override string ToString()
        {
            return $"{Message}\n{StackTrace}";
        }

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

        Type type;
        string arg;

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

        int inputArgsCount;
        int minArgsCount;
        int maxArgsCount;

        public override string Message
        {
            get
            {
                if (inputArgsCount < minArgsCount)
                    return "Not enough arguments";

                if (inputArgsCount > maxArgsCount)
                    return "Too many arguments";

                return "Invalid argument count";
            }
        }
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