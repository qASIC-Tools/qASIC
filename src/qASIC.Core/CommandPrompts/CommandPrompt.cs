namespace qASIC.CommandPrompts
{
    public abstract class CommandPrompt
    {
        public virtual bool CanExecute(qCommandContext context) =>
            true;

        public virtual bool ParseArguments => false;

        public abstract qCommandArgument[] Prepare(qCommandContext context);
    }
}