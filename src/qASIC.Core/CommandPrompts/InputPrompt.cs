namespace qASIC.CommandPrompts
{
    public class InputPrompt : CommandPrompt
    {
        public override bool ParseArguments => true;

        public override CommandArgument[] Prepare(CommandContext context) =>
            context.args;
    }
}