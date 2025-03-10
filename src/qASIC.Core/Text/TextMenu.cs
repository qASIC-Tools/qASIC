using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace qASIC.Text
{
    public class TextMenu<T> : ITextMenu
    {
        public TextMenu(string header = "") : this(header, new List<TextMenuItem<T>>()) { }

        public TextMenu(string header, IEnumerable<TextMenuItem<T>> items, string footer = "")
        {
            Header = header;
            Items = items.ToList();
            Footer = footer;
        }

        public string Header { get; set; }
        public string Footer { get; set; }

        public object DefaultConfirmValue { get; set; } = null;

        public List<TextMenuItem<T>> Items { get; set; } = new List<TextMenuItem<T>>();
        public List<int> Selection { get; set; } = new List<int>();

        public Func<bool> CanCancel;

        /// <summary>
        /// Return new position based on the provided delta. Position will be clamped.
        /// <code>
        /// //Default behaviour
        /// return Position + arg;
        /// </code>
        /// </summary>
        public Func<int, int> HandleMove;

        int _position;
        public int Position
        {
            get => Math.Clamp(_position, -1, Items.Count - 1);
            set => _position = value;
        }

        public void Move(int delta)
        {
            var newPos = Position + delta;
            if (HandleMove != null)
                newPos = HandleMove(delta);

            if (Items.Count == 0)
            {
                Position = 0;
                return;
            }

            Position = Math.Clamp(newPos, 0, Items.Count - 1);
        }

        public object Confirm()
        {
            if (Position == -1) return DefaultConfirmValue;
            var val = Items[Position].OnConfirm?.Invoke(Items[Position].value);
            Selection.Clear();
            return val;
        }

        public bool Cancel()
        {
            if (Selection.Count > 0)
            {
                Selection.Clear();
                return false;
            }

            return CanCancel?.Invoke() != false;
        }

        public bool TryInvokeItemAction(char key, out object result)
        {
            result = null;
            if (!Items.IndexInRange(Position))
                return false;

            var target = Items[Position].actions
                .Where(x => x.key == key)
                .FirstOrDefault();

            if (target == null)
                return false;

            result = target.action.Invoke(Items[Position].value);
            return true;
        }

        public void Select()
        {
            if (Position == -1) return;
            if (!Selection.Contains(Position) && Items[Position].Selectable)
                Selection.Add(Position);
        }

        public void Deselect()
        {
            if (Position == -1) return;
            if (Selection.Contains(Position))
                Selection.Remove(Position);
        }

        public string GenerateMenu()
        {
            var txt = new StringBuilder(Header);

            int startIndex = Position / 8 * 8;
            int endIndex = Math.Min(startIndex + 8, Items.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                txt.Append('\n');
                txt.Append(Position == i ? '>' : '-');
                txt.Append(' ');

                if (Selection.Contains(i))
                    txt.Append("  ");

                txt.Append(Items[i].displayName);
            }

            if (Items.IndexInRange(Position) && Items[Position].actions.Count > 0)
            {
                txt.Append('\n');
                txt.Append(string.Join(" ", Items[Position].actions.Select(x => x.displayName)));
            }

            txt.Append('\n');
            txt.Append(Footer);

            return txt.ToString().Trim();
        }
    }

    public class TextMenu : TextMenu<string>
    {
        public TextMenu(string header = "") : base(header) { }
        public TextMenu(string header, IEnumerable<TextMenuItem<string>> items, string footer = "") : base(header, items, footer) { }
    }
}