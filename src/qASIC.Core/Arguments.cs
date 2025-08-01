using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using qASIC.CommandPrompts;
using qASIC.Parsing;
using qASIC.Logging;

namespace qASIC
{
    public class qCommandContext : IEnumerable<qCommandArgument>
    {
        public qCommandContext() { }
        public qCommandContext(qCommandContext other) : this()
        {
            other?.CopyTo(this);
        }

        public virtual void CopyTo(qCommandContext target)
        {
            target.inputString = inputString;
            target.commandName = commandName;
            target.args = args;
            target.prompt = prompt;
            target.parser = parser;
            target.command = command;
            target.Logs = Logs;
        }

        public string inputString;
        public string commandName;
        public qCommandArgument[] args;
        public CommandPrompt prompt;
        public ModularParser parser;
        public ICommandLogic command;

        public event Action<qLog> OnLog;

        private qLogManager _logs = null;
        public qLogManager Logs
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

        public qCommandArgument this[int index]
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
                throw new qCommandArgsCountException(args.Length, min, max);
        }

        public void CheckArgumentCountMin(int min)
        {
            if (args.Length < min)
                throw new qCommandArgsCountException(args.Length, min, int.MaxValue);
        }

        public void CheckArgumentCountMax(int max)
        {
            if (args.Length > max)
                throw new qCommandArgsCountException(args.Length, 0, max);
        }

        public IEnumerator<qCommandArgument> GetEnumerator() =>
            args
            .AsEnumerable()
            .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            args.GetEnumerator();
    }

    public class qCommandArgument
    {
        public qCommandArgument(string arg, params object[] values) : this(null, arg, values) { }
        public qCommandArgument(ModularParser parser, string arg, params object[] values)
        {
            Parser = parser;
            this.arg = arg;
            this.values = values;
        }

        public string arg;
        public object[] values;
        public ModularParser Parser { get; set; }

        public static explicit operator string(qCommandArgument arg) =>
            arg.arg.ToString();

        public T GetValue<T>() =>
            (T)GetValue(typeof(T));

        public object GetValue(Type type)
        {
            var result = TryGetValue(type, out var obj);
            if (!result) throw new qCommandParseException(type, arg);
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

            throw new qCommandOptionException(arg, Enum.GetValues<T>().Select(x => x.ToString()));
        }

        public T GetOption<T>(IDictionary<string, T> dict)
        {
            if (TryGetOption(dict, out T result))
                return result;

            throw new qCommandOptionException(arg, dict.Select(x => x.Key));
        }

        public int GetOptionIndex(string[] options)
        {
            if (TryGetOptionIndex(options, out int index))
                return index;

            throw new qCommandOptionException(arg, options);
        }

        public override string ToString() =>
            arg;
    }
}
