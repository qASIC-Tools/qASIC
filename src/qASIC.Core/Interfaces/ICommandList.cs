using System;
using System.Collections.Generic;

namespace qASIC;

public interface ICommandList : IEnumerable<ICommandLogic>
{
    ICommandList AddCommand(ICommandLogic command);
    ICommandList AddCommandRange(IEnumerable<ICommandLogic> commands);
    ICommandList RemoveCommand(ICommandLogic command);
    void Clear();

    event Action<IEnumerable<ICommandLogic>> OnCommandsAdded;
    event Action<IEnumerable<ICommandLogic>> OnCommandsRemoved;

    public bool TryGetCommand(string commandName, out ICommandLogic command);

    public IEnumerable<string> GetSortedCommandNames();
}
