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
                if (item.TypeValid(type))
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

        public string ConvertToString(object obj)
        {
            if (obj == null)
                return string.Empty;

            var type = obj.GetType();
            var parser = GetParser(type);
            return parser?.ConvertToString(obj) ?? obj.ToString();
        }

        public T Parse<T>(string s) =>
            (T)Parse(typeof(T), s);

        public object Parse(Type type, string s)
        {
            var parser = GetParser(type);
            return parser?.TryParse(type, s, out object result) == true ?
                result :
                null;
        }

        public bool TryParse<T>(string s, out T result)
        {
            var val = TryParse(typeof(T), s, out var obj);
            result = val ? (T)obj : default;
            return val;
        }

        public bool TryParse(Type type, string s, out object result)
        {
            var parser = GetParser(type);
            result = null;
            return parser?.TryParse(type, s, out result) == true;
        }

        public IEnumerator<ValueParser> GetEnumerator() =>
            Parsers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}