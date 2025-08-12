using qASIC.Console.Commands;
using qASIC.qARK;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace qASIC.Console
{
    public class qConsoleConfig : IConfigurable
    {
        public string name;
        public bool isMain;
        public bool logQDebug;
        public bool traceInCommandExceptions;
        public bool traceInUnknownExceptions;

        public qConsoleTheme theme = new qConsoleTheme();

        #region Log Manager
        public bool saveLogs;
        public string logFilePath;
        public string logFileFormat;
        #endregion

        #region Commands
        public bool addBuiltInCommands;
        public bool findCommands;
        public bool findAttributeCommands;
        public List<Type> commands = new List<Type>();
        #endregion

        public qARKDocument CreateConfig() =>
            new qARKDocument()
                .AddEntry("name", name)
                .AddEntry("isMain", isMain)
                .AddSpace()
                .AddComment("LOGGING")
                .StartGroup("logging")
                    .AddEntry("saveLogs", saveLogs)
                    .AddEntry("logFilePath", logFilePath)
                    .AddEntry("logFileFormat", logFileFormat)
                    .AddSpace()
                    .AddEntry("logQDebug", logQDebug)
                    .AddEntry("traceInCommandExceptions", traceInCommandExceptions)
                    .AddEntry("traceInUnknownExceptions", traceInUnknownExceptions)
                .FinishGroup()
                .AddSpace()
                .AddComment("THEME")
                .StartGroup("logTheme")
                    .AddFromOther(theme?.CreateConfig())
                .FinishGroup()
                .AddSpace()
                .AddComment("COMMANDS")
                .StartGroup("commandList")
                    .AddEntry("addBuiltIn", addBuiltInCommands)
                    .AddEntry("findCommands", findCommands)
                    .AddEntry("findAttributeCommands", findAttributeCommands)
                    .AddArrayEntry("commands", commands)
                .FinishGroup();

        public void LoadConfig(string path) =>
            LoadConfig(new qARKSerializer().Deserialize(File.ReadAllText(path)));

        public void LoadConfig(qARKHolder data)
        {
            name = data.GetValue("name", name);
            isMain = data.GetValue("isMain", isMain);

            saveLogs = data.GetValue("logs.saveLogs", saveLogs);
            logFilePath = data.GetValue("logs.logFilePath", logFilePath);
            logFileFormat  = data.GetValue("logs.logFileFormat", logFileFormat);
            logQDebug = data.GetValue("logs.logQDebug", logQDebug);
            traceInCommandExceptions = data.GetValue("logs.traceInCommandExceptions", traceInCommandExceptions);
            traceInUnknownExceptions = data.GetValue("logs.traceInUnknownExceptions", traceInUnknownExceptions);
            addBuiltInCommands = data.GetValue("commandList.addBuiltIn", addBuiltInCommands);
            findCommands = data.GetValue("commandList.findCommanbds", findCommands);
            findAttributeCommands = data.GetValue("commandList.findAttributeCommands", findAttributeCommands);

            commands.Clear();
            commands.AddRange(data.GetValueArray<string>("commandList.commands")
                .Select(x => Type.GetType(x))
                .Where(x => x != null));
        }

        public qConsole CreateConsole()
        {
            var console = new qConsole();
            ModifyConsole(console);
            return console;
        }

        public void ModifyConsole(qConsole console) =>
            ModifyConsole(console, null);

        public void ModifyConsole(qConsole console, qInstance instance)
        {
            console.Instance = instance;
            console.Name = name;

            switch (isMain)
            {
                case true:
                    console.SetAsMain();
                    break;
                case false:
                    console.UnsetAsMain();
                    break;
            }

            console.LogQDebug = logQDebug;

            console.IncludeStackTraceInCommandExceptions = traceInCommandExceptions;
            console.IncludeStackTraceInUnknownCommandExceptions = traceInUnknownExceptions;

            console.Logs.RawFilePath = saveLogs ? logFilePath : string.Empty;
            console.Logs.FileLogFormat = logFileFormat;

            //COMMANDS
            console.CommandList.Clear();
            if (addBuiltInCommands)
                console.CommandList.AddCommandRange(qCommandList.GetBuiltInCommands());

            //Add manually defined commands
            console.CommandList.AddCommandRange(commands.Except(console.CommandList.Select(x => x.GetType()))
                .Where(x => x.IsAssignableTo(typeof(ICommandLogic)))
                .Select(x => x.GetConstructor(new Type[0])?.Invoke(null))
                .Where(x => x != null)
                .Select(x => (ICommandLogic)x));
        }

        public static qConsoleConfig CreateDefault() =>
            new qConsoleConfig()
            {
                name = "MAIN",
                isMain = true,

                logQDebug = true,
                traceInCommandExceptions = false,
                traceInUnknownExceptions = true,

                saveLogs = true,
                logFilePath = "%APP%/logs.txt",
                logFileFormat = "[%TIME:HH:mm:ss.fff%] [%TYPE%] %MESSAGE%",

                addBuiltInCommands = true,
                findCommands = true,
                findAttributeCommands = true,
            };
    }
}
