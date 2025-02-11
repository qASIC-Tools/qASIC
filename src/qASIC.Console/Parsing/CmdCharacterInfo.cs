namespace qASIC.Console.Parsing
{
    public struct CmdCharacterInfo
    {
        public CmdCharacterInfo(string prefix, string postfix, string commandName, CommandArgument[] arguments)
        {
            this.prefix = prefix;
            this.postfix = postfix;
            this.commandName = commandName;
            this.arguments = arguments;

            scope = Scope.CommandName;
            scopedPosition = 0;
            argumentIndex = 0;
        }

        public enum Scope
        {
            CommandName,
            Argument,
        }

        public CmdCharacterInfo WithScope(Scope scope, int scopedPosition) =>
            WithScope(scope, scopedPosition, 0);

        public CmdCharacterInfo WithScope(Scope scope, int scopedPosition, int argumentIndex)
        {
            this.scope = scope;
            this.scopedPosition = scopedPosition;
            this.argumentIndex = argumentIndex;
            return this;
        }

        public Scope scope;
        public int scopedPosition;
        public int argumentIndex;

        public string commandName;
        public CommandArgument[] arguments;

        public string prefix;
        public string postfix;
    }
}