using qASIC.Core.QML;
using System;
using System.Collections.Generic;
using System.Linq;

namespace qASIC.QML
{
    public class QmlDocument : QmlHolder
    {
        public QmlDocument() : base() { }
        public QmlDocument(IEnumerable<QmlElement> elements) : base(elements) { }

        public string PathPrefix { get; private set; }

        #region Adding
        public QmlDocument AddElement(QmlElement element)
        {
            Add(element);
            return this;
        }

        public QmlDocument AddEntry(string path, object value) =>
            AddElement(new QmlEntry($"{PathPrefix}{path}", path, value));

        public QmlDocument StartArrayEntry(string path) =>
            AddElement(new QmlEntry($"{PathPrefix}{path}", path, string.Empty)
            {
                IsArrayStart = true
            });

        public QmlDocument AddArrayItem(object value)
        {
            var prevEntry = GetLastElementOfType<QmlEntry>();
            return AddElement(new QmlEntry(prevEntry?.Path ?? string.Empty, prevEntry?.RelativePath ?? string.Empty, value)
            {
                IsArrayItem = true,
            });
        }

        public QmlDocument StartGroup(string groupPath)
        {
            PathPrefix = string.IsNullOrWhiteSpace(groupPath) ?
                string.Empty :
                $"{groupPath}.";

            return AddElement(new QmlGroupBorder(groupPath));
        }

        public QmlDocument FinishGroup() =>
            StartGroup(string.Empty);

        public QmlDocument AddComment(string comment) =>
            AddElement(new QmlComment(comment));

        public QmlDocument AddSpace() =>
            AddElement(new QmlSpace());
        #endregion

        #region Setting Single Value
        public QmlDocument SetValue(string path, object value)
        {
            var entry = GetEntry(path);
            if (entry == null)
            {
                AddEntry(path, value);
                return this;
            }

            entry.Value = value?.ToString() ?? string.Empty;
            return this;
        }

        public QmlDocument SetValues(string path, object[] values)
        {
            var entries = GetEntries(path);
            var valueCount = values.Count();
            int min = Math.Min(valueCount, entries.Length);
            int max = Math.Max(valueCount, entries.Length);
            bool moreValues = valueCount > entries.Length;

            for (int i = 0; i < min; i++)
                entries[i].Value = values[i]?.ToString() ?? string.Empty;

            if (moreValues)
            {
                var insertAtIndex = entries.Length > 0 ?
                    Elements.IndexOf(entries[min - 1]) + 1 :
                    -1;

                var relativePath = entries.Length > 0 ?
                    entries[min - 1].RelativePath :
                    path;

                if (insertAtIndex == -1)
                {
                    //Finish if in group
                    if (GetLastElementOfType<QmlGroupBorder>()?.IsEnding == false)
                    {
                        FinishGroup();
                        AddSpace();
                    }

                    StartArrayEntry(path);
                    insertAtIndex = Elements.Count;
                }

                for (int i = min; i < max; i++)
                {
                    Elements.Insert(insertAtIndex, new QmlEntry(path, relativePath, values[i])
                    {
                        IsArrayItem = true,
                    });
                    insertAtIndex++;
                }

                return this;
            }

            for (int i = min; i < max; i++)
                Elements.Remove(entries[i]);

            return this;
        }
        #endregion
    }
}