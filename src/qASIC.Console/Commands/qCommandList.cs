using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Linq;

namespace qASIC.Console.Commands
{
    public class qCommandList : ICommandList
    {
        private List<RegisteredCommand> Commands { get; set; } = new List<RegisteredCommand>();

        public event Action<IEnumerable<ICommand>> OnCommandsAdded;
        public event Action<IEnumerable<ICommand>> OnCommandsRemoved;

        /// <summary>Adds command to the list.</summary>
        /// <param name="command">Command to add.</param>
        /// <returns>Returns itself.</returns>
        public qCommandList AddCommand(ICommand command) =>
            AddCommandRange(new ICommand[] { command });

        /// <summary>Adds commands to the list.</summary>
        /// <param name="command">Collection of commands to add.</param>
        /// <returns>Returns itself.</returns>
        public qCommandList AddCommandRange(IEnumerable<ICommand> commands)
        {
            Commands.AddRange(commands.Select(x => new RegisteredCommand(x)));
            OnCommandsAdded?.Invoke(commands);
            return this;
        }

        /// <summary>Adds all built-in commands to the list.</summary>
        /// <returns>Returns itself.</returns>
        public qCommandList AddBuiltInCommands() =>
            AddCommandRange(new ICommand[]
            {
                new BuiltIn.Cmd_Clear(),
                new BuiltIn.Cmd_Echo(),
                new BuiltIn.Cmd_Exit(),
                new BuiltIn.Cmd_Hello(),
                new BuiltIn.Cmd_Help(),
                new BuiltIn.Cmd_Version(),
                new BuiltIn.Cmd_Unstick(),
                new BuiltIn.Cmd_RemoteInfo(),
            });

        /// <summary>Finds and adds commands to the list that use <see cref="qCommandMarkAttribute"/>.</summary>
        /// <returns>Returns itself.</returns>
        public qCommandList FindCommands() =>
            FindCommands<qCommandMarkAttribute>();

        /// <summary>Finds and adds commands to the list that use the specified attribute.</summary>
        /// <typeparam name="T">Type of attribute used by target commands.</typeparam>
        /// <returns>Returns itself.</returns>
        public qCommandList FindCommands<T>() where T : Attribute =>
            FindCommands(typeof(T));

        /// <summary>Finds and adds commands to the list that use the specified attribute.</summary>
        /// <param name="type">Type of attribute used by target commands.</param>
        /// <returns>Returns itself.</returns>
        public qCommandList FindCommands(Type type)
        {
            var commandTypes = TypeFinder.FindClassesWithAttribute(type, BindingFlags.Public | BindingFlags.NonPublic)
                .Where(x => typeof(ICommand).IsAssignableFrom(x));

            var commands = TypeFinder.CreateConstructorsFromTypes<ICommand>(commandTypes)
                .Where(x => x != null);

            AddCommandRange(commands);

            return this;
        }

        /// <summary>Finds and adds methods, properties and fields marked with <see cref="qCommandAttribute"/>.</summary>
        /// <returns>Returns itself.</returns>
        public qCommandList FindAttributeCommands() =>
            FindAttributeCommands<qCommandAttribute>();

        /// <summary>Finds and adds methods, properties and fields marked with the specified attribute.</summary>
        /// <returns>Returns itself.</returns>
        public qCommandList FindAttributeCommands<T>() where T : qCommandAttribute =>
            FindAttributeCommands(typeof(T));

        /// <summary>Finds and adds methods, properties and fields marked with the specified attribute.</summary>
        /// <returns>Returns itself.</returns>
        public qCommandList FindAttributeCommands(Type type)
        {
            const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var methods = TypeFinder.FindMethodsWithAttribute(type, bindingFlags)
                .Select(x => x as MemberInfo);

            var fields = TypeFinder.FindFieldsWithAttribute(type, bindingFlags)
                .Select(x => x as MemberInfo);

            var properties = TypeFinder.FindPropertiesWithAttribute(type, bindingFlags)
                .Select(x => x as MemberInfo);

            var targets = methods
                .Concat(fields)
                .Concat(properties);

            var addedCommands = new List<ICommand>();
            foreach (var member in targets)
            {
                var attr = (qCommandAttribute)member.GetCustomAttribute(type);
                if (attr == null) continue;

                var commandName = attr.Name.ToLower();

                var commandExists = Commands
                    .Any(x => x.command.CommandName.ToLower() == commandName);

                var command = commandExists ?
                    (qAttributeCommand)Commands.Where(x => x.command.CommandName == commandName).First().command :
                    null;

                command ??= new qAttributeCommand()
                {
                    CommandName = commandName,
                };

                var memberTarget = qAttributeCommand.Target.CreateFromMember(member);

                command.Targets.Add(memberTarget);

                if (commandExists) continue;

                addedCommands.Add(command);
                Commands.Add(new RegisteredCommand(command));
            }

            OnCommandsAdded?.Invoke(addedCommands);
            return this;
        }

        public qCommandList RemoveCommand(ICommand command)
        {
            var target = Commands.Where(x => x.command == command)
                .FirstOrDefault();

            if (target != null)
            {
                Commands.Remove(target);
                OnCommandsRemoved?.Invoke(new ICommand[] { command });
            }

            return this;
        }

        /// <summary>Tries to find command.</summary>
        /// <param name="commandName">Name of the command, doesn't need to be lowercase.</param>
        /// <param name="command">Found command.</param>
        /// <returns>Returns if it found a command.</returns>
        public bool TryGetCommand(string commandName, out ICommand command)
        {
            commandName = commandName?.ToLower();

            var targets = Commands
                .Where(x => x.names.Contains(commandName))
                .Select(x => x.command);

            command = targets.FirstOrDefault();
            return command != null;
        }

        ICommandList ICommandList.AddCommand(ICommand command) =>
            AddCommand(command);

        ICommandList ICommandList.AddCommandRange(IEnumerable<ICommand> commands) =>
            AddCommandRange(commands);

        ICommandList ICommandList.RemoveCommand(ICommand command) =>
            RemoveCommand(command);

        public void Clear()
        {
            Commands.Clear();
        }

        public IEnumerable<string> GetSortedCommandNames() =>
            Commands.SelectMany(x => x.names)
                .OrderBy(x => x)
                .Distinct();

        public IEnumerator<ICommand> GetEnumerator() =>
            Commands
            .Select(x => x.command)
            .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public ICommand this[int index]
        {
            get => Commands[index].command;
        }

        public int Length =>
            Commands.Count;

        class RegisteredCommand
        {
            public RegisteredCommand(ICommand command)
            {
                this.command = command;

                names = command.Aliases
                    .Prepend(command.CommandName)
                    .Select(x => x.ToLower())
                    .ToArray();
            }

            public string[] names;
            public ICommand command;
        }
    }
}