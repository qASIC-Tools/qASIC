using System;
using System.Collections.Generic;

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
        public List<TextMenuItemAction<T>> actions = new List<TextMenuItemAction<T>>();
        public Func<T, object> OnConfirm;

        public TextMenuItem<T> ChangeSelectable(bool newValue)
        {
            Selectable = newValue;
            return this;
        }

        public bool Selectable { get; set; } = false;
    }
}