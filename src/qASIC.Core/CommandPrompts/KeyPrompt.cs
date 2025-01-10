using System.Collections.Generic;
using System.Linq;

namespace qASIC.CommandPrompts
{
    public class KeyPrompt : CommandPrompt
    {
        public enum NavigationKey
        {
            None,
            Up,
            Down,
            Left,
            Right,
            Confirm,
            Cancel,
        }

        public static readonly Map<string, NavigationKey> keyNames = new Map<string, NavigationKey>(new Dictionary<string, NavigationKey>()
        {
            [""] = NavigationKey.None,
            ["up"] = NavigationKey.Up,
            ["down"] = NavigationKey.Down,
            ["left"] = NavigationKey.Left,
            ["right"] = NavigationKey.Right,
            ["confirm"] = NavigationKey.Confirm,
            ["cancel"] = NavigationKey.Cancel,
        });

        public NavigationKey Key { get; private set; } = NavigationKey.None;

        public override bool CanExecute(CommandContext context) =>
            context.inputString.Length > 0;

        public override CommandArgument[] Prepare(CommandContext context)
        {
            string s = context.inputString.First().ToString();

            if (keyNames.Forward.TryGetValue(context.inputString.ToLower(), out var key))
            {
                Key = key;
                s = context.inputString.ToLower();
            }

            var values = s.Length == 1 ?
                new object[] { s[0], s } :
                new object[] { s };

            return new CommandArgument[]
            {
                new CommandArgument(s, values),
            };
        }
    }
}