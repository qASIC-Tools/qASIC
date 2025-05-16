using qASIC.qARK;
using System;
using System.Collections.Generic;

namespace qASIC.Console
{
    public class qConsoleConfig : IConfigurable
    {
        public string name;
        public bool isMain;

        #region Logging
        public bool saveLogs;
        public string logPath;
        
        #endregion

        #region Commands
        public bool addBuiltInCommands;
        public List<Type> commands { get; set; }
        #endregion

        public qARKDocument CreateConfig() =>
            new qARKDocument();

        public void LoadConfig(qARKHolder data)
        {
            throw new NotImplementedException();
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
        }
    }
}
