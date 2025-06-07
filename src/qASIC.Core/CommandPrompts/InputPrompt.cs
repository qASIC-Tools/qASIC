namespace qASIC.CommandPrompts
{
    public class InputPrompt : CommandPrompt
    {
        public override bool ParseArguments => true;

        public override qCommandArgument[] Prepare(qCommandContext context) =>
            context.args;
    }
}