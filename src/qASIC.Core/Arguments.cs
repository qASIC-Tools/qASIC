using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using qASIC.CommandPrompts;
using qASIC.Parsing;

namespace qASIC
{
    public class CommandContext : IEnumerable<CommandArgument>
    {
        public CommandContext() { }
        public CommandContext(CommandContext other) : this()
        {
            inputString = other.inputString;
            commandName = other.commandName;
            args = other.args;
            prompt = other.prompt;
        }

        public string inputString;
        public string commandName;
        public CommandArgument[] args;
        public CommandPrompt prompt;

        public event Action<qLog> OnLog;

        private LogManager _logs = null;
        public LogManager Logs 
        {
            get => _logs; 
            set
            {
                if (_logs != null)
                    _logs.OnLog -= OnLog;

                _logs = value;

                if (_logs != null)
                    _logs.OnLog += OnLog;
            }
        }

        public CommandArgument this[int index]
        {
            get => args[index];
            set => args[index] = value;
        }

        public int Length => args.Length;

        public void CheckArgumentCount(int count) =>
            CheckArgumentCount(count, count);

        public void CheckArgumentCount(int min, int max)
        {
            bool valid = min <= args.Length && args.Length <= max;

            if (!valid)
                throw new CommandArgsCountException(args.Length, min, max);
        }

        public void CheckArgumentCountMin(int min)
        {
            if (args.Length < min)
                throw new CommandArgsCountException(args.Length, min, int.MaxValue);
        }

        public void CheckArgumentCountMax(int max)
        {
            if (args.Length > max)
                throw new CommandArgsCountException(args.Length, 0, max);
        }

        public IEnumerator<CommandArgument> GetEnumerator() =>
            args
            .AsEnumerable()
            .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            args.GetEnumerator();
    }

    public class CommandArgument
    {
        public CommandArgument(string arg, params object[] values) : this(null, arg, values) { }
        public CommandArgument(ModularParser parser, string arg, params object[] values)
        {
            Parser = parser;
            this.arg = arg;
            this.values = values;
        }

        public string arg;
        public object[] values;
        public ModularParser Parser { get; set; }

        public static explicit operator string(CommandArgument arg) =>
            arg.arg.ToString();

        public T GetValue<T>() =>
            (T)GetValue(typeof(T));

        public object GetValue(Type type)
        {
            var result = TryGetValue(type, out var obj);
            if (!result) throw new CommandParseException(type, arg);
            return obj;
        }

        public bool TryGetValue<T>(out T value)
        {
            var result = TryGetValue(typeof(T), out var obj);
            value = (T)obj;
            return result;
        }

        public bool TryGetValue(Type type, out object value)
        {
            foreach (var item in values)
            {
                var itemType = item.GetType();
                if (!type.IsAssignableFrom(itemType)) continue;
                value = item;
                return true;
            }

            return Parser.TryParse(type, arg, out value);
        }

        public bool CanGetValue<T>() =>
            CanGetValue(typeof(T));

        public bool CanGetValue(Type type) =>
            TryGetValue(type, out _);

        public override string ToString() =>
            arg;
    }
}
