namespace qASIC.Console
{
    public static class qInstanceExtensions
    {
        public static qConsoleInstanceManager UseConsole(this qInstance instance)
        {
            instance.AppInfo.RegisterSystem(qConsole.SYSTEM_NAME, qConsole.SYSTEM_VERSION);
            var consoleManager = new qConsoleInstanceManager(instance.RemoteInspectorServer);
            instance.Services.Add(consoleManager);
            return consoleManager;
        }

        public static qConsoleInstanceManager GetConsoleInstanceManager(this qInstance instance) =>
            instance.Services.Get<qConsoleInstanceManager>();

        public static void RegisterConsoleInstance(this qInstance instance, qConsole console) =>
            instance.GetConsoleInstanceManager()
                .RegisterConsole(console);
    }
}
