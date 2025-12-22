namespace qASIC.Console.Parsing;

public struct CmdCharacterInfo(string prefix, string postfix, string commandName, qCommandArgument[] arguments)
{
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

    public Scope scope = Scope.CommandName;
    public int scopedPosition = 0;
    public int argumentIndex = 0;

    public string commandName = commandName;
    public qCommandArgument[] arguments = arguments;

    public string prefix = prefix;
    public string postfix = postfix;
}
