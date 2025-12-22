using qASIC.Console.Autocomplete;
using System;
using System.Collections.Generic;
using System.Linq;

namespace qASIC.Console.Ui;

public class qConsoleTextUi
{
    public qConsoleTextUi() : this(null)
    {

    }

    public qConsoleTextUi(qConsole console)
    {
        Target = console;
    }

    public AutocompleteEngine AC { get; set; } = new BlockAutocompleteEngine(null);

    private qConsole _target;
    public qConsole Target 
    { 
        get => _target; 
        set
        {
            _target = value;
            AC.Console = _target;
        }
    }

    private string _inputString;
    public string InputString 
    { 
        get => _inputString;
        set
        {
            _inputString = value;
            OnInputStringChanged?.Invoke(_inputString);

            if (_inputCursorPosition > _inputString.Length)
                InputCursorPosition = _inputCursorPosition;
        }
    }

    private int _inputCursorPosition;
    public int InputCursorPosition 
    { 
        get => _inputCursorPosition;
        set
        {
            _inputCursorPosition = Math.Clamp(value, 0, _inputString.Length);
            OnInputCursorPositionChanged?.Invoke(_inputCursorPosition);
        }
    }

    public event Action<string> OnInputStringChanged;
    public event Action<int> OnInputCursorPositionChanged;

    /// <summary>Attempts to autocomplete. You should typically call this whenever the tab key is pressed.</summary>
    public virtual void Autocomplete()
    {
        if (Target == null || AC == null)
            return;

        (InputString, InputCursorPosition) = AC.Autocomplete(InputString, InputCursorPosition);
    }

    public virtual object Execute()
    {
        object obj = null;
        if (Target != null)
            obj = Target.Execute(InputString);

        InputString = string.Empty;
        return obj;
    }

    /// <summary>Creates a list of logs from the target console to be displayed in the ui.</summary>
    /// <param name="logCount">The limit of logs to get. When set to -1, the limit will be ignored.</param>
    /// <returns>Returns a list of logs.</returns>
    public List<qLog> GetLogs(int logCount = 128)
    {
        var logs = new List<qLog>();

        if (Target != null)
        {
            logs.AddRange(Target.Logs.Logs
                .Where(x => x.sticky));

            for (int i = 0; i < Target.Logs.Logs.Count && (i < logCount || logCount < 0); i++)
            {
                var log = Target.Logs.Logs[Target.Logs.Logs.Count - i - 1];

                if (log.logType == LogType.Clear)
                    break;

                if (log.sticky)
                    continue;

                logs.Insert(0, log);
            }
        }

        return logs;
    }
}
