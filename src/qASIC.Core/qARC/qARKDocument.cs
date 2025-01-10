using qASIC.Core.qARK;
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
                    if (GetLastElementOfType<qARKGroupBorder>()?.IsEnding == false)
                    {
                        FinishGroup();
                        AddSpace();
                    }

                    StartArrayEntry(path);
                    insertAtIndex = Elements.Count;
                }

                for (int i = min; i < max; i++)
                {
                    Elements.Insert(insertAtIndex, new qARKEntry(path, relativePath, Parser.ConvertToString(values[i]) ?? string.Empty)
                    {
                        Parser = Parser,
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