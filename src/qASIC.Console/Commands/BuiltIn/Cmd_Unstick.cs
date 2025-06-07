namespace qASIC.Console.Commands.BuiltIn
{
    public class Cmd_Unstick : qBuiltinCommandLogic
    {
        protected override string DefaultCommandName => "unstick";
        protected override string DefaultDescription => "Unsticks all logs marked as sticky.";

        public override object Run(qConsoleCommandContext context)
        {
            context.CheckArgumentCount(0);

            var count = 0;
            foreach (var item in context.console.Logs)
            {
                if (!item.sticky) continue;
                item.UnStick();
                count++;
            }

            context.Logs.Log($"Unsticked {count} logs.");
            return null;
        }
    }
}
