using qASIC.Console.Autocomplete;
using qASIC.qARK;

namespace qASIC.Console.Commands.BuiltIn
{
    public class Cmd_Hello : qBuiltinCommandLogic
    {
        protected override string DefaultCommandName => "helloworld";
        protected override string DefaultDescription => "Hello World!";
        protected override string DefaultDetailedDescription => "Logs a test message to the console.";
        protected override string[] DefaultAliases => new string[] { "hello" };

        public override ACData CommandAutocomplete => new ACData()
            .AddVariant().Finish();

        public string HelloMessage { get; set; } = "Hello World :)";
        public string Tag { get; set; } = null;
        public qColor Color { get; set; } = qColor.Green;

        public override object Run(qConsoleCommandContext context)
        {
            context.CheckArgumentCount(0);
            
            var log = qLog.CreateNow(HelloMessage, LogType.Application, Color);
            log.tag = Tag;

            context.Logs.Log(log);
            return null;
        }

        public override void LoadConfig(qARKHolder data)
        {
            base.LoadConfig(data);
            HelloMessage = data.GetValue("helloMessage", "Hello World :)");
            Tag = data.GetValue<string>("tag", null);
            Color = data.GetValue("color", qColor.Green);
        }
    }
}