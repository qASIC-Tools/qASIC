using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using qASIC.CommandPrompts;
using qASIC.Console.Autocomplete;

using SysConsole = System.Console;

namespace qASIC.Console
{
    /// <summary>Class responsible for displaying and reading information from a Console Application window for a <see cref="GameConsole"/> instance.</summary>
    public class SystemConsoleUI
    {
        public SystemConsoleUI() : this(new GameConsole("MAIN")) { }

        public SystemConsoleUI(qInstance instance) : this(new GameConsole("MAIN")) { }

        public SystemConsoleUI(GameConsole console)
        {
            Console = console;
            inputs = new List<string>(new string[1]);
        }

        GameConsole _console;
        /// <summary>Console which will be used by the interface.</summary>
        public GameConsole Console
        {
            get => _console;
            set
            {
                if (_console == value) return;

                if (_console != null)
                {
                    _console.Logs.OnLog -= WriteLog;
                    _console.Logs.OnUpdateLog -= UpdateLog;
                }

                previousLog = null;
                previousLogMessage = string.Empty;
                _console = value;

                if (autocomplete != null)
                    autocomplete.Console = value;

                if (_console != null)
                {
                    _console.Logs.OnLog += WriteLog;
                    _console.Logs.OnUpdateLog += UpdateLog;
                }
            }
        }

        private AutocompleteEngine autocomplete = new BlockAutocompleteEngine(null);
        public AutocompleteEngine Autocomplete
        {
            get => autocomplete;
            set
            {
                autocomplete = value;
                autocomplete.Console = _console;
            }
        }

        /// <summary>Format string used for converting logs to text. See <see cref="qLog.ToString(string)"/>.</summary>
        public string LogFormat { get; set; } = "[%TIME:HH:mm:ss.fff%] [%TYPE:App,Usr,Int,Clr%] %MESSAGE%";

        /// <summary>Determines if user input should be read in <see cref="StartReading(bool)"/>. By setting this to false, interface will stop reading after the next command.</summary>
        public bool CanRead { get; set; } = true;

        /// <summary>Is the console currently reading input from the command line.</summary>
        public bool IsReading { get; set; }

        /// <summary>Gets invoked before executing a command. If false, command will not be executed.</summary>
        public event Func<string, bool> CanExecute;

        /// <summary>Gets invoked before a command string starts being processed, can be used to modify</summary>
        public event Func<string, string> ProcessCommandString;

        string previousLogMessage = string.Empty;
        qLog previousLog = null;

        private void WriteLog(qLog log)
        {
            if (log.logType == LogType.Clear)
            {
                SysConsole.Clear();
                SysConsole.Write(InputString);
                previousLogMessage = string.Empty;
                previousLog = null;
                return;
            }

            var txt = CreateLogText(log);

            previousLogMessage = log.message;
            previousLog = log;

            SysConsole.Write(new string('\b', InputCursorPosition));
            SysConsole.Write(new string(' ', InputString.Length));
            SysConsole.Write(new string('\b', InputString.Length));

            SysConsole.WriteLine(ColorText(txt, Console.GetLogColor(log)));

            SysConsole.Write(InputString);
            SysConsole.Write(new string('\b', InputString.Length - InputCursorPosition));
        }

        private void UpdateLog(qLog log)
        {
            //Ignore if clear
            if (log.logType == LogType.Clear)
                return;

            //If it wasn't the previous log, just log
            if (previousLog != log)
            {
                WriteLog(log);
                return;
            }

            var txt = CreateLogText(log);

            //Clear input line
            SysConsole.Write(new string('\b', InputCursorPosition));
            SysConsole.Write(new string(' ', InputString.Length));
            SysConsole.Write(new string('\b', InputString.Length));

            //Calculate previous message length
            var consoleWidth = SysConsole.BufferWidth;
            var lineLength = previousLogMessage.Split('\n')
                .Select(x => (x.Length - 1) / consoleWidth + 1)
                .Sum();
            var top = Math.Max(0, SysConsole.CursorTop - lineLength);

            //Clear previous message
            SysConsole.CursorTop = top;
            SysConsole.Write(new string(' ', lineLength * consoleWidth));
            SysConsole.CursorTop = top;
            SysConsole.CursorLeft = 0;

            //Write new message
            SysConsole.Write(txt);
            previousLogMessage = log.message;

            //Restore input line
            SysConsole.WriteLine('\b');
            SysConsole.Write(InputString);
            SysConsole.Write(new string('\b', InputString.Length - InputCursorPosition));
        }

        /// <summary>Starts reading user input from the console window.</summary>
        /// <param name="readOnce">If true, reading will not be repeated.</param>
        public void StartReading(bool readOnce = false)
        {
            Task.WaitAll(StartReadingAsync(readOnce));
        }

        /// <summary>Starts reading user input from the console window asynchronously.</summary>
        /// <param name="readOnce">If true, reading will not be repeated.</param>
        public async Task StartReadingAsync(bool readOnce = false)
        {
            if (!CanRead)
                return;

            do
            {
                IsReadingInput = true;

                currentInput = inputs.Count - 1;

                bool isDone = false;
                while (!isDone)
                {
                    await Task.Delay(10);

                    while (SysConsole.KeyAvailable && !isDone)
                        isDone |= HandleKey(SysConsole.ReadKey(true));
                }

                var cmd = FinalizeInput();

                if (CanExecute?.Invoke(cmd) == false)
                    continue;

                cmd = ProcessCommandString?.Invoke(cmd) ?? cmd;
                await Console.ExecuteAsync(cmd);
            }
            while (CanRead && !readOnce);
        }

        #region Input
        public bool IsReadingInput { get; private set; }

        public int InputCursorPosition { get; private set; } = 0;
        public string InputString { get; private set; } = string.Empty;

        public int PreviousInputsLimit { get; set; } = 128;

        List<string> previousInputs = new List<string>();
        List<string> inputs;
        int currentInput = 0;

        string GetInputAfterCursor() =>
            InputString.Substring(InputCursorPosition, InputString.Length - InputCursorPosition);

        string GetInputBeforeCursor() =>
            InputString.Substring(0, InputCursorPosition);

        bool HandleKey(ConsoleKeyInfo key)
        {
            if (Console.ReturnedValue is KeyPrompt prompt)
            {
                if (key.Modifiers != 0)
                    return false;

                var promptKey = key.Key switch
                {
                    ConsoleKey.UpArrow => KeyPrompt.NavigationKey.Up,
                    ConsoleKey.DownArrow => KeyPrompt.NavigationKey.Down,
                    ConsoleKey.LeftArrow => KeyPrompt.NavigationKey.Left,
                    ConsoleKey.RightArrow => KeyPrompt.NavigationKey.Right,
                    ConsoleKey.Enter => KeyPrompt.NavigationKey.Confirm,
                    ConsoleKey.Escape => KeyPrompt.NavigationKey.Cancel,
                    ConsoleKey.Delete => KeyPrompt.NavigationKey.Delete,
                    ConsoleKey.Tab => KeyPrompt.NavigationKey.Switch,
                    _ => KeyPrompt.NavigationKey.None,
                };

                InputString = KeyPrompt.keyNames.Backward[promptKey];

                if (promptKey == KeyPrompt.NavigationKey.None)
                {
                    if (!char.IsLetterOrDigit(key.KeyChar) &&
                        !char.IsWhiteSpace(key.KeyChar) &&
                        !char.IsPunctuation(key.KeyChar) &&
                        !char.IsSymbol(key.KeyChar))
                        return false;

                    InputString = key.KeyChar.ToString();
                }

                return true;
            }

            //Handling modifiers
            if (key.Modifiers != 0 && key.Modifiers != ConsoleModifiers.Shift)
            {
                if (key.Modifiers == ConsoleModifiers.Control ||
                    key.Modifiers == ConsoleModifiers.Alt)
                {
                    //Navigation
                    if (key.Key == ConsoleKey.LeftArrow)
                    {
                        var length = Math.Min(WordBeforeLength() + 1, InputCursorPosition);
                        InputCursorPosition -= length;
                        SysConsole.Write(new string('\b', length));

                        return false;
                    }

                    if (key.Key == ConsoleKey.RightArrow)
                    {
                        var length = Math.Min(WordAfterLength(), InputString.Length - InputCursorPosition);
                        SysConsole.Write(InputString.Substring(InputCursorPosition, length));
                        InputCursorPosition += length;

                        return false;
                    }

                    //Deleting
                    if (key.Key == ConsoleKey.Backspace)
                    {
                        DeleteBeforeCursor(WordBeforeLength());
                        return false;
                    }

                    if (key.Key == ConsoleKey.Delete)
                    {
                        DeleteAfterCursor(WordAfterLength());
                        return false;
                    }
                }

                return false;
            }

            //Apply
            if (key.Key == ConsoleKey.Enter && key.Modifiers == 0)
                return true;

            if (!(Console.ReturnedValue is TextPrompt))
            {
                //Swapping current input to previous
                if (key.Key == ConsoleKey.UpArrow && key.Modifiers == 0)
                {
                    ChangeInput(currentInput - 1);
                    return false;
                }

                if (key.Key == ConsoleKey.DownArrow && key.Modifiers == 0)
                {
                    ChangeInput(currentInput + 1);
                    return false;
                }

                //Autocorrect
                if (key.Key == ConsoleKey.Tab && key.Modifiers == 0)
                {
                    SysConsole.Write(new string('\b', InputCursorPosition));
                    SysConsole.Write(new string(' ', InputString.Length));
                    SysConsole.Write(new string('\b', InputString.Length));
                    (InputString, InputCursorPosition) = Autocomplete.Autocomplete(InputString, InputCursorPosition);
                    SysConsole.Write(InputString);
                    SysConsole.Write(new string('\b', InputString.Length - InputCursorPosition));
                    return false;
                }
            }


            //Navigation
            if (key.Key == ConsoleKey.LeftArrow)
            {
                if (InputCursorPosition > 0)
                {
                    InputCursorPosition -= 1;
                    SysConsole.Write('\b');
                }

                return false;
            }

            if (key.Key == ConsoleKey.RightArrow)
            {
                if (InputCursorPosition < InputString.Length)
                {
                    SysConsole.Write(InputString.Substring(InputCursorPosition, 1));
                    InputCursorPosition += 1;
                }

                return false;
            }

            //Deleting
            if (key.Key == ConsoleKey.Backspace)
            {
                DeleteBeforeCursor(1);
                return false;
            }

            if (key.Key == ConsoleKey.Delete)
            {
                DeleteAfterCursor(1);
                return false;
            }

            //Writting
            if (!new int[] {0, 7, 27}.Contains(key.KeyChar))
            {
                var toWrite = InputString.Substring(InputCursorPosition, InputString.Length - InputCursorPosition);

                SysConsole.Write(key.KeyChar);
                SysConsole.Write(toWrite);
                SysConsole.Write(new string('\b', toWrite.Length));

                InputString = InputString.Substring(0, InputCursorPosition) + key.KeyChar + toWrite;
                InputCursorPosition++;
            }

            return false;
        }

        void ChangeInput(int newIndex)
        {
            if (newIndex < 0 || newIndex >= inputs.Count) return;

            inputs[currentInput] = InputString;
            SysConsole.Write(new string('\b', InputCursorPosition));
            SysConsole.Write(new string(' ', InputCursorPosition));
            SysConsole.Write(new string('\b', InputCursorPosition));

            currentInput = newIndex;

            InputString = inputs[currentInput];
            SysConsole.Write(InputString);
            InputCursorPosition = InputString.Length;
        }

        int WordAfterLength()
        {
            var txt = GetInputAfterCursor();
            return txt.Length - txt.TrimStart().Length +
                txt.TrimStart().Split(' ').First().Length;
        }

        int WordBeforeLength()
        {
            var txt = GetInputBeforeCursor();
            return txt.Length - txt.TrimEnd().Length +
                txt.TrimEnd().Split(' ').Last().Length;
        }

        void DeleteBeforeCursor(int amount = 1)
        {
            amount = Math.Min(amount, InputCursorPosition);
            InputCursorPosition -= amount;
            InputString = InputString.Substring(0, InputCursorPosition) + InputString.Substring(InputCursorPosition + amount, InputString.Length - InputCursorPosition - amount);

            SysConsole.Write(new string('\b', amount));

            var delLength = InputString.Length - InputCursorPosition + amount;
            SysConsole.Write(InputString.Substring(InputCursorPosition, delLength - amount) + new string(' ', amount));
            SysConsole.Write(new string('\b', delLength));
        }

        void DeleteAfterCursor(int amount = 1)
        {
            amount = Math.Min(amount, InputString.Length - InputCursorPosition);

            var toWrite = InputString.Substring(InputCursorPosition + amount, InputString.Length - InputCursorPosition - amount);
            SysConsole.Write(toWrite + new string(' ', amount));
            SysConsole.Write(new string('\b', toWrite.Length + amount));
            InputString = InputString.Substring(0, InputCursorPosition) + toWrite;
        }

        string FinalizeInput()
        {
            var cmd = InputString;
            var inputVisible = !(Console.ReturnedValue is CommandPrompt);

            //Finish writing input
            if (inputVisible)
                SysConsole.WriteLine(InputString.Substring(InputCursorPosition, InputString.Length - InputCursorPosition));

            //Clear
            InputString = "";
            InputCursorPosition = 0;
            IsReadingInput = false;

            if (inputVisible)
            {
                //Ignore rest if empty
                if (string.IsNullOrWhiteSpace(cmd))
                    return cmd;

                //Saving previous inputs

                //Add final input and apply
                previousInputs.Add(cmd);
                inputs[inputs.Count - 1] = cmd;

                //Ensure limit
                while (previousInputs.Count > PreviousInputsLimit)
                {
                    currentInput--;
                    previousInputs.RemoveAt(0);
                    inputs.RemoveAt(0);
                }

                //If a previous input was modified and executed, revert to old one
                if (currentInput >= 0)
                    inputs[currentInput] = previousInputs[currentInput];

                inputs.Add(string.Empty);
            }

            return cmd;
        }
        #endregion

        protected string CreateLogText(qLog log) =>
            log.ToString(LogFormat);

        protected string ColorText(string txt, qColor color) =>
            $"\u001b[38;2;{color.red};{color.green};{color.blue}m{txt}\u001b[0m";
        //txt;

        protected string CreateEmptyStringForLog(qLog log) =>
            ColorText(new string(CreateLogText(log).Select(x => char.IsControl(x) ? x : ' ').ToArray()), log.color);
    }
}
