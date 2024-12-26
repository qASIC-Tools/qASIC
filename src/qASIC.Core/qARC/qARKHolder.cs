using qASIC.qARK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace qASIC.Core.qARK
{
    public abstract class qARKHolder : IEnumerable<qARKElement>
    {
        public qARKHolder() { }
        public qARKHolder(IEnumerable<qARKElement> elements)
        {
            Elements = elements.ToList();
            Entries = elements
                .Where(x => x is qARKEntry)
                .Select(x => x as qARKEntry)
                .GroupBy(x => x.Path)
                .ToDictionary(x => x.Key, x => x.ToList());
        }

        protected virtual string PathPrefix => string.Empty;
        protected List<qARKElement> Elements { get; set; } = new List<qARKElement>();
        protected Dictionary<string, List<qARKEntry>> Entries { get; set; } = new Dictionary<string, List<qARKEntry>>();

        #region Entries
        public qARKEntry GetEntry(string path) =>
            Entries.TryGetValue($"{PathPrefix}{path}", out var val) ?
            val.Where(x => !x.IsArrayStart).FirstOrDefault() :
            null;

        public qARKEntry[] GetEntries(string path) =>
            Entries.TryGetValue($"{PathPrefix}{path}", out var val) ?
            val.Where(x => !x.IsArrayStart).ToArray() :
            new qARKEntry[0];

        public T GetLastElementOfType<T>() where T : qARKElement
        {
            for (int i = Elements.Count - 1; i >= 0; i--)
            {
                if (Elements[i] is T el)
                    return el;
            }

            return null;
        }

        public void Add(qARKElement element)
        {
            Elements.Add(element);
            if (element is qARKEntry entry)
            {
                if (!Entries.ContainsKey(entry.Path))
                    Entries.Add(entry.Path, new List<qARKEntry>());

                Entries[entry.Path].Add(entry);
            }
        }

        public void AddRange(IEnumerable<qARKElement> elements)
        {
            Elements.AddRange(elements);
            var entries = elements.Where(x => x is qARKEntry)
                .Select(x => x as qARKEntry);

            foreach (var entry in entries)
            {
                if (!Entries.ContainsKey(entry.Path))
                    Entries.Add(entry.Path, new List<qARKEntry>());

                Entries[entry.Path].Add(entry);
            }
        }
        #endregion

        #region Values
        public T GetValue<T>(string path, T defaultValue = default) =>
            qARKUtility.ParseValue<T>(GetEntry(path)?.Value, defaultValue);

        public object GetValue(string path, Type type, object defaultValue = null) =>
            qARKUtility.ParseValue(type, GetEntry(path)?.Value, defaultValue);

        public bool TryGetValue<T>(string path, out T result) =>
            TryGetValue(path, default, out result);

        public bool TryGetValue<T>(string path, T defaultValue, out T result) =>
            qARKUtility.TryParseValue(GetEntry(path)?.Value, defaultValue, out result);

        public bool TryGetValue(string path, Type type, out object result) =>
            TryGetValue(path, type, default, out result);

        public bool TryGetValue(string path, Type type, object defaultValue, out object result) =>
            qARKUtility.TryParseValue(type, GetEntry(path)?.Value, defaultValue, out result);

        public qARKObject GetObject(string path)
        {
            var obj = new qARKObject($"{PathPrefix}{path}");

            foreach (var item in Entries)
                if (item.Key.StartsWith($"{PathPrefix}{path}"))
                    obj.AddRange(item.Value);

            return obj;
        }

        public List<T> GetValueArray<T>(string path)
        {
            var list = new List<T>();
            foreach (var entry in GetEntries(path))
                if (entry.TryGetValue<T>(out T obj))
                    list.Add(obj);

            return list;
        }

        public List<object> GetValueArray(Type type, string path)
        {
            List<object> list = new List<object>();
            foreach (var entry in GetEntries($"{PathPrefix}{path}"))
                if (entry.TryGetValue(type, out object obj))
                    list.Add(obj);

            return list;
        }

        public List<qARKObject> GetObjectArray(string path)
        {
            var dict = new Dictionary<string, qARKObject>();

            int pathPartsCount = $"{PathPrefix}{path}".Split('.').Length;

            foreach (var item in Entries)
            {
                if (!item.Key.StartsWith($"{PathPrefix}{path}")) continue;
                var parts = item.Key.Split('.');

                if (parts.Length == pathPartsCount) continue;

                var objParts = new string[pathPartsCount + 1];
                Array.Copy(parts, 0, objParts, 0, objParts.Length);
                var objPath = string.Join('.', objParts);

                if (!dict.ContainsKey(objPath))
                    dict.Add(objPath, new qARKObject(objPath));

                dict[objPath].AddRange(item.Value);
            }

            return dict.Values.ToList();
        }
        #endregion

        public void Clear()
        {
            Elements.Clear();
            Entries.Clear();
        }

        public IEnumerator<qARKElement> GetEnumerator() =>
            Elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public int Count =>
            Elements.Count;
    }
}