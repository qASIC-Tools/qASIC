namespace qASIC.CommandPrompts;

public class TextPrompt(object data = null) : CommandPrompt(data)
{
    public string Text { get; private set; }

    public override void Prepare(qCommandContext context)
    {
        Text = context.inputString;
        context.args = [new qCommandArgument(context.inputString, [context.inputString])];
    }
}

public class TextPrompt<T>(T data) : TextPrompt(data)
{
    public T Data
    {
        get => (T)DataObject;
        set => DataObject = value;
    }
}
