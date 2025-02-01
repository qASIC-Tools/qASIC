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

        public bool TryGetValueOverride<T>(ref T value)
        {
            if (TryGetValue(out T val))
            {
                value = val;
                return true;
            }

            return false;
        }

        public bool TryGetValueOverride(Type type, ref object value)
        {
            if (TryGetValue(type, out object val))
            {
                value = val;
                return true;
            }

            return false;
        }

        public List<object> GetAllPossibleValues()
        {
            var list = new List<object>(values);

            foreach (var item in Parser.Parsers)
                if (item.TryParse(arg, out var val))
                    list.Add(val);
                
            return list;
        }

        public bool CanGetValue<T>() =>
            CanGetValue(typeof(T));

        public bool CanGetValue(Type type) =>
            TryGetValue(type, out _);

        public bool TryGetEnum<T>(out T result) where T : struct, Enum
        {
            var values = Enum.GetValues<T>();

            foreach (var item in values)
            {
                if (item.ToString().ToLower() != arg.ToLower()) continue;
                result = item;
                return true;
            }

            result = default;
            return false;
        }

        public bool TryGetOption<T>(IDictionary<string, T> dict, out T result)
        {
            return dict.TryGetValue(arg, out result);
        }

        public bool TryGetOptionIndex(string[] options, out int index)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (arg.ToLower() != options[i].ToLower()) continue;
                index = i;
                return true;
            }

            index = -1;
            return false;
        }

        public T GetEnum<T>() where T : struct, Enum
        {
            if (TryGetEnum<T>(out T result))
                return result;

            throw new CommandOptionException(arg, Enum.GetValues<T>().Select(x => x.ToString()));
        }

        public T GetOption<T>(IDictionary<string, T> dict)
        {
            if (TryGetOption(dict, out T result))
                return result;

            throw new CommandOptionException(arg, dict.Select(x => x.Key));
        }

        public int GetOptionIndex(string[] options)
        {
            if (TryGetOptionIndex(options, out int index))
                return index;

            throw new CommandOptionException(arg, options);
        }

        public override string ToString() =>
            arg;
    }
}
