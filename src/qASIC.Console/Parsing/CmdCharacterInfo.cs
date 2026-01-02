using System.Collections.Generic;

namespace qASIC.Console.Parsing;

public struct CmdCharacterInfo
{
    public enum Scope
    {
        Nothing,
        CommandName,
        Argument,
        Variable,
    }

    public Scope scope;
    public int argumentIndex;
    public IEnumerable<string> avaliableVariables;

    public string commandName;
    public string argument;
    public string variableName;

    public object ParserData { get; set; }
}
