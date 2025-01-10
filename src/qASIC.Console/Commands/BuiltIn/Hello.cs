namespace qASIC.Console.Commands.BuiltIn
{
    public class Hello : GameCommand
    {
        public override string CommandName => "helloworld";
        public override string Description => "Hello World!";
        public override string DetailedDescription => "Logs a test message to the console.";
        public override string[] Aliases => new string[] { "hello" };

        public override object Run(GameCommandContext context)
        {
            context.CheckArgumentCount(0);
            context.Logs.Log("Hello world :)", qColor.Green);
            return null;
        }
    }
}