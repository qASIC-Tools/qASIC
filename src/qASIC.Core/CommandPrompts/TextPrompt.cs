namespace qASIC.CommandPrompts
{
    public class TextPrompt : CommandPrompt
    {
        public TextPrompt(object data = null) : base(data) { }

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

    public class TextPrompt<T> : TextPrompt
    {
        public TextPrompt(T data) : base(data) { }

        public T Data
        {
            get => (T)DataObject;
            set => DataObject = value;
        }
    }
}