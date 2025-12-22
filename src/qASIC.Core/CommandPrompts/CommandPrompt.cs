namespace qASIC.CommandPrompts;

public abstract class CommandPrompt(object data = null)
{
    public object ParserData { get; set; }
    public qCommandContext CommandContext { get; set; }
    public object DataObject { get; set; } = data;

    public virtual bool CanExecute(qCommandContext context) =>
        true;

    public virtual bool ParseArguments => false;

    public abstract void Prepare(qCommandContext context);
}
