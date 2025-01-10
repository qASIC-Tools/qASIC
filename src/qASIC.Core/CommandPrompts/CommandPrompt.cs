namespace qASIC.CommandPrompts
{
    public abstract class CommandPrompt
    {
        public virtual bool CanExecute(CommandContext context) =>
            true;

        public virtual bool ParseArguments => false;

        public abstract CommandArgument[] Prepare(CommandContext context);
    }
}