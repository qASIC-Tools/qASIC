namespace qASIC.CommandPrompts
{
    public class TextPrompt : CommandPrompt
    {
        public string Text { get; private set; }

        public override CommandArgument[] Prepare(CommandContext context)
        {
            Text = context.inputString;
            return new CommandArgument[]
            {
                new CommandArgument(context.inputString, new object[] { context.inputString }),
            };
        }
    }
}