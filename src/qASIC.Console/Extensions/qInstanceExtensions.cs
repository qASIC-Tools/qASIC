namespace qASIC.Console
{
    public static class qInstanceExtensions
    {
        public static InstanceConsoleManager UseConsole(this qInstance instance)
        {
            instance.AppInfo.RegisterSystem(qConsole.SYSTEM_NAME, qConsole.SYSTEM_VERSION);
            var consoleManager = new InstanceConsoleManager(instance.RemoteInspectorServer);
            instance.Services.Add(consoleManager);
            return consoleManager;
        }

        public static InstanceConsoleManager GetConsoleInstanceManager(this qInstance instance) =>
            instance.Services.Get<InstanceConsoleManager>();

        public static void RegisterConsoleInstance(this qInstance instance, qConsole console) =>
            instance.GetConsoleInstanceManager()
                .RegisterConsole(console);
    }
}
