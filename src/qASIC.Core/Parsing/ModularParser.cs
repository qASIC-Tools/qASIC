using System;
using System.Collections;
using System.Collections.Generic;

namespace qASIC.Parsing
{
    public class ModularParser : IEnumerable<ValueParser>
    {
        public ModularParser() : this(ValueParser.CreateStandardParserArray()) { }

        public ModularParser(IEnumerable<ValueParser> enumerable)
        {
            Parsers = new List<ValueParser>(enumerable);
        }

        public List<ValueParser> Parsers { get; private set; }

        public ValueParser<T> GetParser<T>()
        {
            foreach (var item in Parsers)
                if (item is ValueParser<T> parser)
                    return parser;

            return null;
        }

        public ValueParser GetParser(Type type)
        {
            foreach (var item in Parsers)
                if (item.ValueType == type)
                    return item;

            return null;
        }

        public bool TryGetParser<T>(out ValueParser parser)
        {
            parser = GetParser<T>();
            return parser != null;
        }

        public bool TryGetParser(Type type, out ValueParser parser)
        {
            parser = GetParser(type);
            return parser != null;
        }

        public string ConvertToString<T>(T obj)
        {
            if (obj == null)
                return string.Empty;

            var parser = GetParser<T>();
            return parser?.ConvertToString(obj) ?? obj.ToString();
        }

        public string ConvertToString(object obj)
        {
            if (obj == null)
                return string.Empty;

            var parser = GetParser(obj.GetType());
            return parser?.ConvertToString(obj) ?? obj.ToString();
        }

        public T Parse<T>(string s)
        {
            var parser = GetParser<T>();
            return parser?.TryParse(s, out T result) == true ?
                result :
                default;
        }

        public object Parse(Type type, string s)
        {
            var parser = GetParser(type);
            return parser?.TryParse(s, out object result) == true ?
                result :
                null;
        }

        public bool TryParse<T>(string s, out T result)
        {
            var parser = GetParser<T>();
            result = default;
            return parser?.TryParse(s, out result) == true;
        }

        public bool TryParse(Type type, string s, out object result)
        {
            var parser = GetParser(type);
            result = null;
            return parser?.TryParse(s, out result) == true;
        }

        public IEnumerator<ValueParser> GetEnumerator() =>
            Parsers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}