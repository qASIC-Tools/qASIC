using System;

namespace qASIC.Text
{
    public class TextMenuItem<T>
    {
        public TextMenuItem() : this(default) { }
        public TextMenuItem(T value) : this(value.ToString(), value) { }
        public TextMenuItem(string displayName, T value, Func<T, object> onConfirm = null)
        {
            this.displayName = displayName;
            this.value = value;
            OnConfirm = onConfirm;
        }

        public string displayName;
        public T value;
        public Func<T, object> OnConfirm;

        public TextMenuItem<T> ChangeSelectable(bool newValue)
        {
            Selectable = Selectable;
            return this;
        }

        public bool Selectable { get; set; } = true;
    }

    public class TextMenuItem : TextMenuItem<string>
    {
        public TextMenuItem() : base(default) { }
        public TextMenuItem(string value) : base(value.ToString(), value) { }
        public TextMenuItem(string displayName, string value, Func<string, object> onConfirm = null) : base() { }
    }
}