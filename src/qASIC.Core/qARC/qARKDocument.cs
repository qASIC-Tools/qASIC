using qASIC.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace qASIC.qARK
{
    public class qARKDocument : qARKHolder
    {
        public const string FILE_EXTENSION = "qark";

        public qARKDocument() : base() { }
        public qARKDocument(ModularParser parser) : base(parser) { }
        public qARKDocument(IEnumerable<qARKElement> elements) : base(elements) { }
        public qARKDocument(ModularParser parser, IEnumerable<qARKElement> elements) : base(parser, elements) { }

        public string NewElementPrefix { get; private set; }

        #region Adding
        public qARKDocument AddElement(qARKElement element)
        {
            Add(element);
            return this;
        }

        public qARKDocument AddEntry(string path, object value) =>
            AddElement(new qARKEntry($"{NewElementPrefix}{path}", path, Parser?.ConvertToString(value) ?? string.Empty)
            {
                Parser = Parser,
            });

        public qARKDocument StartArrayEntry(string path) =>
            AddElement(new qARKEntry($"{NewElementPrefix}{path}", path, string.Empty)
            {
                Parser = Parser,
                IsArrayStart = true
            });

        public qARKDocument AddArrayItem(object value)
        {
            var prevEntry = GetLastElementOfType<qARKEntry>();
            return AddElement(new qARKEntry(prevEntry?.Path ?? string.Empty, prevEntry?.RelativePath ?? string.Empty, Parser?.ConvertToString(value) ?? string.Empty)
            {
                Parser = Parser,
                IsArrayItem = true,
            });
        }

        public qARKDocument StartGroup(string groupPath)
        {
            NewElementPrefix = string.IsNullOrWhiteSpace(groupPath) ?
                string.Empty :
                $"{groupPath}.";

            return AddElement(new qARKGroupBorder(groupPath));
        }

        public qARKDocument FinishGroup() =>
            StartGroup(string.Empty);

        public qARKDocument AddComment(string comment) =>
            AddElement(new qARKComment(comment));

        public qARKDocument AddSpace() =>
            AddElement(new qARKSpace());
        #endregion

        #region Setting Single Value
        public qARKDocument SetValue(string path, object value)
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

        public qARKDocument SetValues(string path, object[] values)
        {
            var entries = GetEntries(path);
            int min = Math.Min(values.Length, entries.Length);
            int max = Math.Max(values.Length, entries.Length);
            bool moreValues = values.Length > entries.Length;

            //If there are no existing values
            if (values.Length == 0)
            {
                var group = Elements.Where(x => x is qARKGroupBorder)
                    .Select(x => x as qARKGroupBorder)
                    .Where(x => !x.IsEnding && path.StartsWith($"{x.Path}."))
                    .MaxBy(x => x.Path.Split('.').Length);

                int index = NewElementInGroupIndex(group);
                var relativePath = path.Substring(0, group?.Path.Length + 1 ?? 0);
                Entries.Add(path, new List<qARKEntry>());

                var start = new qARKEntry(path, relativePath, string.Empty)
                {
                    Parser = Parser,
                    IsArrayStart = true,
                };

                Elements.Insert(index, start);
                Entries[path].Add(start);

                for (int i = 0; i < values.Length; i++)
                {
                    var entry = new qARKEntry(path, relativePath, Parser.ConvertToString(values))
                    {
                        Parser = Parser,
                    };

                    Elements.Insert(index + i + 1, entry);
                    Entries[path].Add(entry);
                }

                return this;
            }

            for (int i = 0; i < min; i++)
                entries[i].Value = values[i]?.ToString() ?? string.Empty;

            if (moreValues)
            {
                var target = entries.Last();
                var index = Elements.IndexOf(target) + 1;

                for (int i = min; i < max; i++)
                {
                    var entry = new qARKEntry(target.Path, target.RelativePath, Parser.ConvertToString(values[i]))
                    {
                        Parser = Parser,
                        IsArrayItem = target.IsArrayItem || target.IsArrayStart,
                    };

                    Elements.Insert(index + i, entry);
                    Entries[path].Add(entry);
                }

                return this;
            }

            for (int i = min; i < max; i++)
                Elements.Remove(entries[i]);

            return this;
        }
        #endregion

        #region Modifying
        private int NewElementInGroupIndex(qARKGroupBorder group)
        {
            if (group?.IsEnding == false)
                group = FindEndOfGroup(group);

            if (group == null || Elements.Contains(group))
                return PreviousNonSpaceElement(Elements.Count - 1) + 1;

            return Elements.IndexOf(group);
        }

        private int PreviousNonSpaceElement(int index)
        {
            while (index >= 0 && Elements[index] is qARKSpace)
                index--;

            return index;
        }

        private int NextNonSpaceElement(int index)
        {
            while (index < Elements.Count && Elements[index] is qARKSpace)
                index++;

            return index;
        }

        private qARKGroupBorder FindEndOfGroup(qARKGroupBorder group)
        {
            if (group == null)
                return null;

            int index = Elements.IndexOf(group);
            if (index == -1) return null;

            for (index += 1; index < Elements.Count; index++)
                if (Elements[index] is qARKGroupBorder)
                    break;

            return index < Elements.Count ?
                Elements[index] as qARKGroupBorder :
                null;
        }

        private void EnsureCorrectPrefix()
        {
            var lastgroup = GetLastElementOfType<qARKGroupBorder>();
            if (lastgroup.IsEnding)
            {
                NewElementPrefix = string.Empty;
                return;
            }

            NewElementPrefix = $"{lastgroup.Path}.";
        }

        private void RemoveGroupBorder(qARKGroupBorder group)
        {
            var index = Elements.IndexOf(group);
            if (index == -1) return;

            do
            {
                Elements.RemoveAt(index);
                if (group.IsEnding)
                    index--;
            }
            while (Elements.IndexInRange(index) && Elements[index] is qARKSpace);
        }
        #endregion
    }
}