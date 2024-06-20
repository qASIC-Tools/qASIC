﻿namespace qASIC.Console.Commands.Prompts
{
    public abstract class CommandPrompt
    {
        public virtual bool CanExecute(CommandArgs args) =>
            true;

        public virtual bool ParseArguments => false;

        public abstract ConsoleArgument[] Prepare(CommandArgs args);
    }
}