namespace qASIC.CommandPrompts
{
    public class TextPrompt : CommandPrompt
    {
        public string Text { get; private set; }

        public override qCommandArgument[] Prepare(qCommandContext context)
        {
            Text = context.inputString;
            return new qCommandArgument[]
            {
                new qCommandArgument(context.inputString, new object[] { context.inputString }),
            };
        }
    }
}