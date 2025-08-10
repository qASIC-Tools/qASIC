namespace qASIC.CommandPrompts
{
    public abstract class CommandPrompt
    {
        public CommandPrompt(object data = null)
        {
            DataObject = data;
        }

        public qCommandContext Context { get; set; }
        public object DataObject { get; set; }

        public virtual bool CanExecute(qCommandContext context) =>
            true;

        public virtual bool ParseArguments => false;

        public abstract void Prepare(qCommandContext context);
    }
}