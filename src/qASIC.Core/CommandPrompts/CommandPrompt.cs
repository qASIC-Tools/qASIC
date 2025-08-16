using qASIC.Logging;

namespace qASIC.CommandPrompts
{
    public abstract class CommandPrompt
    {
        public CommandPrompt(object data = null)
        {
            DataObject = data;
        }

        public object ParserData { get; set; }
        public qCommandContext CommandContext { get; set; }
        public object DataObject { get; set; }

        public virtual bool CanExecute(qCommandContext context) =>
            true;

        public virtual bool ParseArguments => false;

        public abstract void Prepare(qCommandContext context);
    }
}