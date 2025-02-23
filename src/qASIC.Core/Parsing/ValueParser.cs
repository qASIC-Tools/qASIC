using System;

namespace qASIC.Parsing
{
    public abstract class ValueParser
    {
        public ValueParser() { }

        public static ValueParser[] CreateStandardParserArray() =>
            new ValueParser[]
            {
                new qColorParser(),
                new DateTimeParser(),
                new TimeSpanParser(),
                new EnumParser(),
                new IntParser(),
                new UIntParser(),
                new FloatParser(),
                new DoubleParser(),
                new DecimalParser(),
                new LongParser(),
                new UlongParser(),
                new ByteParser(),
                new SByteParser(),
                new ShortParser(),
                new UShortParser(),
                new BoolParser(),
                new CharParser(),
                new StringParser(),
            };

        public abstract Type ValueType { get; }

        public virtual bool TypeValid(Type type) =>
            ValueType == type;

        public bool TryParse(string s, out object result) =>
            TryParse(ValueType, s, out result);

        public abstract bool TryParse(Type type, string s, out object result);

        public virtual string ConvertToString(object obj) =>
            obj?.ToString() ?? string.Empty;
    }

    public abstract class ValueParser<T> : ValueParser
    {
        public override Type ValueType => typeof(T);

        public override bool TryParse(Type type, string s, out object result)
        {
            var value = TryParse(s, out T parseResult);
            result = parseResult;
            return value;
        }

        public abstract bool TryParse(string s, out T result);

        public override string ConvertToString(object obj) =>
            ConvertToString((T)obj);

        public virtual string ConvertToString(T obj) =>
            obj?.ToString() ?? string.Empty;
    }
}