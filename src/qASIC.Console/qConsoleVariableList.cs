using System.Collections;
using System.Collections.Generic;

namespace qASIC.Console
{
    public class qConsoleVariableList : IEnumerable<KeyValuePair<string, object>>
    {
        public qConsoleVariableList() { }
        public qConsoleVariableList(IEnumerable<KeyValuePair<string, object>> other)
        {
            Set(other);
        }

        private Dictionary<string, object> values = new Dictionary<string, object>();

        public void Get(string name) =>
            values.GetValueOrDefault(name);

        public void Set(string name, object value)
        {
            if (value == null && values.ContainsKey(name))
            {
                values.Remove(name);
                return;
            }

            values.SetOrAdd(name, value);
        }

        public void Set(IEnumerable<KeyValuePair<string, object>> enumerable)
        {
            if (enumerable == this)
                return;

            foreach (var item in enumerable)
                Set(item.Key, item.Value);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() =>
            values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}