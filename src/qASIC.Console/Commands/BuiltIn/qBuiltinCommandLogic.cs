using qASIC.CmdAutocomplete;
using qASIC.Console.Autocomplete;
using qASIC.qARK;
using System;

namespace qASIC.Console.Commands.BuiltIn
{
    public abstract class qBuiltinCommandLogic : ICommandLogic, ISupportsAutocomplete, IConfigurable
    {
        public qBuiltinCommandLogic()
        {
            CommandName = DefaultCommandName;
            Aliases = DefaultAliases;
            Description = DefaultDescription;
            DetailedDescription = DefaultDetailedDescription;
        }

        protected abstract string DefaultCommandName { get; }
        public string CommandName { get; set; }

        protected virtual string[] DefaultAliases => Array.Empty<string>();
        public string[] Aliases { get; set; }

        protected virtual string DefaultDescription { get; }
        public string Description { get; set; }

        protected virtual string DefaultDetailedDescription { get; }
        public string DetailedDescription { get; set; }

        public virtual ACData CommandAutocomplete { get; }

        public object Run(qCommandContext context) =>
            Run(context as qConsoleCommandContext);

        public abstract object Run(qConsoleCommandContext context);

        public virtual void LoadConfig(qARKHolder data)
        {
            CommandName = data.GetValue("commandName", DefaultCommandName);
            Aliases = data.GetValueArray<string>("aliases").ToArray();
            Description = data.GetValue("description", DefaultDescription);
            DetailedDescription = data.GetValue("detailedDescription", DefaultDetailedDescription);
        }

        public virtual qARKDocument CreateConfig() =>
            new qARKDocument()
                .AddEntry("commandName", CommandName)
                .AddEntry("aliases", Aliases)
                .AddEntry("description", Description)
                .AddEntry("detailedDescription", DetailedDescription);
    }
}
