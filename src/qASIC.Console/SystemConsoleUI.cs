using qASIC.CommandPrompts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SysConsole = System.Console;

namespace qASIC.Console
{
    /// <summary>Class responsible for displaying and reading information from a Console Application window for a <see cref="GameConsole"/> instance.</summary>
    public class SystemConsoleUI
    {
        public SystemConsoleUI() : this(new GameConsole("MAIN")) { }

        public SystemConsoleUI(qInstance instance) : this(new GameConsole(instance, "MAIN")) { }

        public SystemConsoleUI(GameConsole console)
        {
            Console = console;
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

                VisibleLogs.Clear();
                _console = value;

                if (_console != null)
                {
                    _console.Logs.OnLog += WriteLog;
                    _console.Logs.OnUpdateLog += UpdateLog;
                }
            }
        }

        /// <summary>Format of a log, where:
        /// <list type="bullet">
        /// <item>{0} - <see cref="qLog.message"/></item>
        /// <item>{1} - <see cref="qLog.time"/></item>
        /// <item>{2} - <see cref="qLog.logType"/></item>
        /// </list>
        /// </summary>
        public string LogFormat { get; set; } = "[{1}] [{2}] {0}";

        /// <summary>String used for formatting <see cref="qLog.time"/>.</summary>
        public string TimeFormat { get; set; } = "HH:mm:ss.fff";

        /// <summary>Determines if user input should be read in <see cref="StartReading(bool)"/>. By setting this to false, interface will stop reading after the next command.</summary>
        public bool CanRead { get; set; }

        /// <summary>Is the console currently reading input from the command line.</summary>
        public bool IsReading { get; set; }

        private Dictionary<qLog, LogData> VisibleLogs { get; set; } = new Dictionary<qLog, LogData>();

        /// <summary>Gets invoked before executing a command. If false, command will not be executed.</summary>
        public event Func<string, bool> CanExecute;

        /// <summary>Gets invoked before a command string starts being processed, can be used to modify</summary>
        public event Func<string, string> ProcessCommandString;

        private void WriteLog(qLog log)
        {
            if (log.logType == LogType.Clear)
            {
                SysConsole.Clear();
                VisibleLogs.Clear();
                return;
            }

            var txt = CreateLogText(log);

            //Do not add log to visible logs if reading
            //if we don't do this, we might freeze until
            //reading is done
            if (!IsReading)
            {
                VisibleLogs.Add(log, new LogData()
                {
                    consoleTop = SysConsole.CursorTop,
                    emptyString = CreateEmptyStringForLog(log),
                });
            }

            SysConsole.WriteLine(ColorText(txt, Console.GetLogColor(log)));
        }

        private void UpdateLog(qLog log)
        {
            //Ignore if clear
            if (log.logType == LogType.Clear)
                return;

            //Print if console is reading (prevent
            //from freezing until done) or if log
            //isn't remembered
            if (IsReading || !VisibleLogs.ContainsKey(log))
            {
                WriteLog(log);
                return;
            }

            var txt = CreateLogText(log);

            var top = SysConsole.CursorTop;
            var left = SysConsole.CursorLeft;

            var logData = VisibleLogs[log];

            SysConsole.CursorTop = logData.consoleTop;
            SysConsole.CursorLeft = 0;

            //Override with garbage data, not sure why it doesn't
            //work with spaces
            SysConsole.Write(logData.emptyString.Replace(' ', '@'));

            SysConsole.CursorTop = logData.consoleTop;
            SysConsole.CursorLeft = 0;

            //Clear
            SysConsole.Write(logData.emptyString);

            SysConsole.CursorTop = logData.consoleTop;
            SysConsole.CursorLeft = 0;

            //Write new log
            SysConsole.Write(ColorText(txt, Console.GetLogColor(log)));

            if (SysConsole.CursorTop >= top)
                top = SysConsole.CursorTop + 1;

            SysConsole.CursorTop = top;
            SysConsole.CursorLeft = left;
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
            CanRead = !readOnce;

            while (CanRead)
            {
                IsReadingInput = true;
                inputs.Add(string.Empty);
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
        }

        #region Input
        public bool IsReadingInput { get; private set; }

        public int InputCursorPosition { get; private set; } = 0;
        public string InputString { get; private set; } = string.Empty;

        public int PreviousInputsLimit { get; set; } = 3;

        List<string> previousInputs = new List<string>();
        List<string> inputs = new List<string>();
        int currentInput = 0;

        string GetInputAfterCursor() =>
            InputString.Substring(InputCursorPosition, InputString.Length - InputCursorPosition);

        string GetInputBeforeCursor() =>
            InputString.Substring(0, InputCursorPosition);

        bool HandleKey(ConsoleKeyInfo key)
        {
            if (Console.ReturnedValue is KeyPrompt prompt)
            {
                var promptKey = key.Key switch
                {
                    ConsoleKey.UpArrow => KeyPrompt.NavigationKey.Up,
                    ConsoleKey.DownArrow => KeyPrompt.NavigationKey.Down,
                    ConsoleKey.LeftArrow => KeyPrompt.NavigationKey.Left,
                    ConsoleKey.RightArrow => KeyPrompt.NavigationKey.Right,
                    ConsoleKey.Enter => KeyPrompt.NavigationKey.Confirm,
                    ConsoleKey.Escape => KeyPrompt.NavigationKey.Cancel,
                    _ => KeyPrompt.NavigationKey.None,
                };

                InputString = KeyPrompt.keyNames.Backward[promptKey];

                if (promptKey == KeyPrompt.NavigationKey.None)
                {
                    if (!char.IsSymbol(key.KeyChar))
                        return false;

                    InputString = key.KeyChar.ToString();
                }

                return true;
            }

            //Apply
            if (key.Key == ConsoleKey.Enter)
                return true;

            //Swapping current input to previous
            if (key.Key == ConsoleKey.UpArrow)
            {
                ChangeInput(currentInput - 1);
                return false;
            }

            if (key.Key == ConsoleKey.DownArrow)
            {
                ChangeInput(currentInput + 1);
                return false;
            }

            //Navigation
            if (key.Key == ConsoleKey.LeftArrow)
            {
                var length = key.Modifiers == ConsoleModifiers.Control || key.Modifiers == ConsoleModifiers.Alt ?
                    WordBeforeLength() + 1 :
                    1;

                length = Math.Min(length, InputCursorPosition);
                InputCursorPosition -= length;
                SysConsole.Write(new string('\b', length));

                return false;
            }

            if (key.Key == ConsoleKey.RightArrow)
            {
                var length = key.Modifiers == ConsoleModifiers.Control || key.Modifiers == ConsoleModifiers.Alt ?
                    WordAfterLength() :
                    1;

                length = Math.Min(length, InputString.Length - InputCursorPosition);
                SysConsole.Write(InputString.Substring(InputCursorPosition, length));
                InputCursorPosition += length;

                return false;
            }

            //Deleting
            if (key.Key == ConsoleKey.Backspace)
            {
                DeleteBeforeCursor(key.Modifiers == ConsoleModifiers.Control || key.Modifiers == ConsoleModifiers.Alt ?
                    WordBeforeLength() :
                    1);
                return false;
            }

            if (key.Key == ConsoleKey.Delete)
            {
                DeleteAfterCursor(key.Modifiers == ConsoleModifiers.Control || key.Modifiers == ConsoleModifiers.Alt ?
                    WordAfterLength() :
                    1);
                return false;
            }

            //Writting
            if (key.KeyChar != 0)
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

            //Finish writing input
            SysConsole.WriteLine(InputString.Substring(InputCursorPosition, InputString.Length - InputCursorPosition));

            //Clear
            InputString = "";
            InputCursorPosition = 0;
            IsReadingInput = false;

            //Saving previous inputs
            if (!string.IsNullOrWhiteSpace(cmd))
            {
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
            }

            return cmd;
        }
        #endregion

        protected string CreateLogText(qLog log) =>
            string.Format(LogFormat, log.message, log.time.ToString(TimeFormat), log.logType);

        protected string ColorText(string txt, qColor color) =>
            $"\u001b[38;2;{color.red};{color.green};{color.blue}m{txt}\u001b[0m";
        //txt;

        protected string CreateEmptyStringForLog(qLog log) =>
            ColorText(new string(CreateLogText(log).Select(x => char.IsControl(x) ? x : ' ').ToArray()), log.color);

        class LogData
        {
            public int consoleTop;
            public string emptyString;
        }
    }
}
