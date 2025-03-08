using System;

namespace qASIC.Text
{
    public class TextMenuItemAction<T>
    {
        public Func<T, object> action;
        public char key;
        public string displayName;
    }
}