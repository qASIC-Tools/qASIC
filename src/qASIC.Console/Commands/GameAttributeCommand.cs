using qASIC.Console.Commands.Attributes;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qASIC.Console.Commands
{
    public class GameAttributeCommand : ICommand
    {
        public GameAttributeCommand() : this(string.Empty) { }
        public GameAttributeCommand(string commandName)
        {
            CommandName = commandName;
        }

        public string CommandName { get; set; }

        public string[] Aliases => Targets
            .Select(x => x.attr.Aliases)
            .Where(x => x != null)
            .SelectMany(x => x)
            .Distinct()
            .ToArray();

        public string Description => Targets
            .Select(x => x.attr.Description)
            .Where(x => x != null)
            .FirstOrDefault();

        public string DetailedDescription => Targets
            .Select(x => x.attr.DetailedDescription)
            .Where(x => x != null)
            .FirstOrDefault();

        public List<Target> Targets { get; set; } = new List<Target>();

        public object Run(CommandContext context)
        {
            var gameContext = context as GameCommandContext;

            var maxArgLimit = Targets
                .Select(x => x.maxArgsCount)
                .Max();

            var minArgLimit = Targets
                .Select(x => x.minArgsCount)
                .Min();

            gameContext.CheckArgumentCount(minArgLimit, maxArgLimit);

            CommandArgument[] cmdArgs = gameContext.args
                .ToArray();

            var targets = Targets
                .Where(x => cmdArgs.Length >= x.minArgsCount && cmdArgs.Length <= x.maxArgsCount)
                .ToArray();

            List<Type>[] supportedArgTypes = new List<Type>[maxArgLimit]
                .Select(x => new List<Type>())
                .ToArray();

            foreach (var target in targets)
                for (int i = 0; i < target.argTypes.Length; i++)
                    supportedArgTypes[i].Add(target.argTypes[i]);

            for (int i = 0; i < cmdArgs.Length; i++)
                cmdArgs[i].values = cmdArgs[i].values
                    .Where(x => supportedArgTypes[i].Contains(x.GetType()) || x is string)
                    .ToArray();

            object returnValue = null;

            int closestMatchCorrectArgsCount = -1;
            Target closestMatch = null;

            if (FindCommandAndTryRun(new List<object>()))
                return returnValue;

            throw new CommandParseException(closestMatch?.argTypes[closestMatchCorrectArgsCount], gameContext[closestMatchCorrectArgsCount + 1].arg);

            bool FindCommandAndTryRun(List<object> values, bool first = true)
            {
                if (values.Count < cmdArgs.Length)
                {
                    var index = values.Count;
                    values.Add(new object());
                    foreach (var value in cmdArgs[index].GetAllPossibleValues())
                    {
                        values[index] = value;
                        if (FindCommandAndTryRun(values, false))
                            return true;

                        if (index == cmdArgs.Length - 1 && RunFromValues(values))
                            return true;
                    }
                }

                if (cmdArgs.Length == 0 && first && RunFromValues(new List<object>()))
                    return true;

                return false;
            }

            bool RunFromValues(List<object> values)
            {
                var valueTypes = values
                    .Select(x => x.GetType())
                    .ToArray();

                foreach (var target in targets)
                {
                    var finalValues = new List<object>(values);

                    var targetArgTypes = new Type[valueTypes.Length];
                    Array.Copy(target.argTypes, targetArgTypes, targetArgTypes.Length);

                    if (target.contextType != null)
                        finalValues.Insert(0, gameContext);

                    var parameterCount = target.maxArgsCount;

                    if (target.contextType != null)
                        parameterCount++;

                    while (finalValues.Count() < parameterCount)
                        finalValues.Add(Type.Missing);

                    int argCount = 0;
                    for (; argCount < valueTypes.Length; argCount++)
                        if (valueTypes[argCount] != targetArgTypes[argCount])
                            break;

                    if (argCount > closestMatchCorrectArgsCount)
                    {
                        closestMatch = target;
                        closestMatchCorrectArgsCount = argCount;
                    }

                    if (argCount != valueTypes.Length)
                        continue;

                    returnValue = target.Invoke(finalValues.ToArray(), gameContext, targets.Length == 1);
                    return true;
                }

                return false;
            }
        }

        public abstract class Target
        {
            public Target(MemberInfo memberInfo)
            {
                this.memberInfo = memberInfo;
                attr = memberInfo.GetCustomAttribute<CommandAttribute>()!;
                targetAttr = memberInfo.GetCustomAttributes<CommandTargetAttribute>()
                    .ToArray();
            }

            public static Target CreateFromMember(MemberInfo memberInfo)
            {
                switch (memberInfo)
                {
                    case MethodInfo methodInfo:
                        return new MethodTarget(methodInfo);
                    case FieldInfo fieldInfo:
                        return new FieldTarget(fieldInfo);
                    case PropertyInfo propertyInfo:
                        return new PropertyTarget(propertyInfo);
                    default:
                        return null;
                }
            }

            public object Invoke(object[] values, GameCommandContext context, bool isSingle = false)
            {
                var targetType = memberInfo.DeclaringType!;
                var targets = targetAttr
                    .SelectMany(x => x.GetTargets(targetType))
                    .Where(x => x != null)
                    .Distinct();

                if (attr.UseRegisteredTargets)
                {
                    var regTargets = context.console.Targets
                        .Where(x => targetType.IsAssignableFrom(x.GetType()));

                    targets = targets
                        .Concat(regTargets);
                }

                var singleTarget = targets.Count() == 1;

                if (IsStatic)
                {
                    return ExecuteInConsole(() =>
                    {
                        return InvokeForItem(null, values, context);
                    });
                }

                object val = null;
                foreach (var item in targets)
                {
                    LogExecuteBegin(context, item);
                    val = ExecuteInConsole(() =>
                    {
                        return InvokeForItem(item, values, context);
                    });
                }

                return singleTarget ? val : null;

                object ExecuteInConsole(Func<object> func)
                {
                    var obj = context.console.Execute(context.commandName, () =>
                    {
                        try
                        {
                            return func?.Invoke();
                        }
                        catch (TargetInvocationException e)
                        {
                            if (e.InnerException != null)
                                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();

                            throw;
                        }
                    }, context.Logs, false);

                    if (obj is Task task && (!isSingle || targets.Count() > 1))
                        Task.Run(() => context.console.ExecuteAsync(context.commandName, task, context.Logs, false));

                    return obj;
                }
            }

            protected abstract bool IsStatic { get; }

            protected abstract object InvokeForItem(object item, object[] values, GameCommandContext context);

            protected void LogExecuteBegin(GameCommandContext context, object target) =>
                context.console.Log($"Executing command for target '{target ?? "NULL"}'");

            public MemberInfo memberInfo;
            public CommandAttribute attr;
            public CommandTargetAttribute[] targetAttr;
            public Type[] argTypes;
            public int minArgsCount;
            public int maxArgsCount;
            /// <summary>Whenever target has <see cref="GameCommandContext"/> as the first parameter</summary>
            public Type contextType;
        }

        public class MethodTarget : Target
        {
            public MethodTarget(MethodInfo methodInfo) : base(methodInfo)
            {
                this.methodInfo = methodInfo;

                var parameters = methodInfo.GetParameters();

                contextType = null;
                if (parameters.Length > 0 && parameters[0].ParameterType.IsAssignableTo(typeof(CommandContext)))
                    contextType = parameters[0].ParameterType;

                if (contextType != null)
                    parameters = parameters
                        .Skip(1)
                        .ToArray();

                minArgsCount = parameters
                    .Where(x => !x.IsOptional)
                    .Count();

                maxArgsCount = parameters.Length;

                argTypes = parameters
                    .Select(x => x.ParameterType)
                    .ToArray();
            }

            MethodInfo methodInfo;

            protected override bool IsStatic => methodInfo.IsStatic;

            protected override object InvokeForItem(object item, object[] values, GameCommandContext context)
            {
                return methodInfo.Invoke(item, values);
            }
        }

        public class FieldTarget : Target
        {
            public FieldTarget(FieldInfo fieldInfo) : base(fieldInfo)
            {
                this.fieldInfo = fieldInfo;
                contextType = null;
                minArgsCount = 0;
                maxArgsCount = 1;
                argTypes = new Type[] { fieldInfo.FieldType! };
            }

            FieldInfo fieldInfo;

            protected override bool IsStatic => fieldInfo.IsStatic;

            protected override object InvokeForItem(object item, object[] values, GameCommandContext context)
            {
                if (values[0] == Type.Missing)
                    return fieldInfo.GetValue(item);

                fieldInfo.SetValue(item, values[0]);
                return null;
            }
        }

        public class PropertyTarget : Target
        {
            public PropertyTarget(PropertyInfo propertyInfo) : base(propertyInfo)
            {
                this.propertyInfo = propertyInfo;
                contextType = null;
                minArgsCount = 0;
                maxArgsCount = 1;
                argTypes = new Type[] { propertyInfo.PropertyType! };
            }

            PropertyInfo propertyInfo;

            protected override bool IsStatic => propertyInfo.GetAccessors(true)[0].IsStatic;

            protected override object InvokeForItem(object item, object[] values, GameCommandContext context)
            {
                if (values[0] == Type.Missing)
                    return propertyInfo.GetValue(item);

                propertyInfo.SetValue(item, values[0]);
                return null;
            }
        }
    }
}