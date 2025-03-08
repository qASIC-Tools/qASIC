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
            Delete,
            Switch,
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
            ["delete"] = NavigationKey.Delete,
            ["switch"] = NavigationKey.Switch,
        });

        public NavigationKey Key { get; private set; } = NavigationKey.None;
        public char Character { get; private set; }

        public override bool CanExecute(CommandContext context) =>
            context.inputString.Length > 0;

        public override CommandArgument[] Prepare(CommandContext context)
        {
            string s = context.inputString.FirstOrDefault().ToString();

            if (keyNames.Forward.TryGetValue(context.inputString.ToLower(), out var key))
            {
                Key = key;
                s = context.inputString.ToLower();
            }

            if (s.Length == 1)
                Character = s[0];

            var values = s.Length == 1 ?
                new object[] { s[0], s } :
                new object[] { s };

            return new CommandArgument[]
            {
                new CommandArgument(s, values),
            };
        }

        public object UseTextMenu(Text.ITextMenu menu)
        {
            switch (Key)
            {
                case NavigationKey.Up:
                    menu.Move(-1);
                    break;
                case NavigationKey.Down:
                    menu.Move(1);
                    break;
                case NavigationKey.Left:
                    menu.Deselect();
                    break;
                case NavigationKey.Right:
                    menu.Select();
                    break;
                case NavigationKey.Confirm:
                    return menu.Confirm();
                case NavigationKey.Cancel:
                    return menu.Cancel() ? null : new KeyPrompt();
            }

            if (menu.TryInvokeItemAction(Character, out var itemActionResult))
                return itemActionResult;

            return new KeyPrompt();
        }
    }
}