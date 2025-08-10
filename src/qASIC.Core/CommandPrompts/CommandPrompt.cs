namespace qASIC.CommandPrompts
{
    public abstract class CommandPrompt
    {
        public CommandPrompt(object data = null)
        {
            DataObject = data;
        }

        public virtual bool CanExecute(qCommandContext context) =>
            true;

        public object ParserData { get; set; }
        public qCommandContext context;
        public virtual bool ParseArguments => false;
        public object DataObject { get; set; }

        public abstract qCommandArgument[] Prepare(qCommandContext context);
    }
}