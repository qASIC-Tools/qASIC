using qASIC.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace qASIC.qARK;

/// <summary>A document containing qARK entries, used for serialization.</summary>
public class qARKDocument : qARKHolder
{
    public const string FILE_EXTENSION = "qark";

    public qARKDocument() : base() { }
    public qARKDocument(ModularParser parser) : base(parser) { }
    public qARKDocument(IEnumerable<qARKElement> elements) : base(elements) { }
    public qARKDocument(ModularParser parser, IEnumerable<qARKElement> elements) : base(parser, elements) { }

    public string NewElementPathPrefix { get; private set; }

    #region Adding
    public qARKDocument AddElement(qARKElement element)
    {
        if (element is qARKGroupBorder group)
        {
            NewElementPathPrefix = string.IsNullOrWhiteSpace(group.AbsolutePath) ?
                string.Empty :
                $"{group.AbsolutePath}.";
        }

        if (element is qARKEntry entry)
        {
            // Additional logic for array item entry
            if (entry.IsArrayItem)
            {
                entry.RelativePath = string.Empty;
                var prevEntry = GetLastElementOfType<qARKEntry>();

                if (prevEntry != null)
                {
                    // If previous entry's path starts differently than
                    // the non-relative path of this element, reset
                    // the path prefix by closing the group
                    if (!prevEntry.AbsolutePath.StartsWith(NewElementPathPrefix))
                        AddElement(new qARKGroupBorder());

                    // Copy relative path from previous entry
                    prevEntry.RelativePath = prevEntry.AbsolutePath[NewElementPathPrefix.Length..];
                }
            }

            // For any type of entry
            entry.AbsolutePath = $"{NewElementPathPrefix}{entry.RelativePath}";
            entry.Parser = Parser;
        }

        Add(element);
        return this;
    }

    public qARKDocument AddEntry(string path, object value) =>
        AddElement(new qARKEntry(path, Parser?.ConvertToString(value) ?? string.Empty));

    public qARKDocument StartArrayEntry(string path) =>
        AddElement(new qARKEntry(path, string.Empty)
        {
            IsArrayStart = true
        });

    public qARKDocument AddArrayEntryFromValues(string path, IEnumerable<object> values)
    {
        StartArrayEntry(path);
        foreach (var item in values)
            AddArrayItem(item);

        return this;
    }

    public qARKDocument AddArrayItem(object value) =>
        AddElement(new qARKEntry(string.Empty, Parser?.ConvertToString(value) ?? string.Empty)
        {
            IsArrayItem = true,
        });

    public qARKDocument AddGroupStart(string groupPath) =>
        AddElement(new qARKGroupBorder(groupPath));

    public qARKDocument AddGroupEnd() =>
        AddGroupStart(string.Empty);

    public qARKDocument AddComment(string comment) =>
        AddElement(new qARKComment(comment));

    public qARKDocument AddSpace(int count = 1) =>
        AddElement(new qARKSpace(count));

    public qARKDocument AddFromOther(qARKHolder other)
    {
        if (other == null)
            return this;

        foreach (var item in other)
        {
            switch (item)
            {
                case qARKEntry entry:
                    if (entry.IsArrayStart)
                        StartArrayEntry(entry.RelativePath);
                    else if (entry.IsArrayItem)
                        AddArrayItem(entry.Value);
                    else
                        AddEntry(entry.RelativePath, entry.Value);
                    break;
                case qARKGroupBorder border:
                    if (border.IsEnding)
                        AddGroupEnd();
                    else
                        AddGroupStart(border.RelativePath);
                    break;
                case qARKComment comment:
                    AddComment(comment.Comment);
                    break;
                case qARKSpace space:
                    AddSpace(space.Count);
                    break;
                default:
                    AddElement(item);
                    break;
            }
        }
        return this;
    }
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
        var entries = GetEntries(path, includeWithoutValue: true);
        var valueEntries = entries.Where(x => !x.IsArrayStart).ToArray();
        int min = Math.Min(values.Length, valueEntries.Length);
        int max = Math.Max(values.Length, valueEntries.Length);
        bool moreValues = values.Length > valueEntries.Length;

        //If there are no existing values
        if (entries.Length == 0)
        {
            var group = Elements.Where(x => x is qARKGroupBorder)
                .Select(x => x as qARKGroupBorder)
                .Where(x => !x.IsEnding && path.StartsWith($"{x.AbsolutePath}."))
                .MaxBy(x => x.AbsolutePath.Split('.').Length);

            int index = NewElementInGroupIndex(group);
            var prefixLength = group?.AbsolutePath.Length + 1 ?? 0;
            var relativePath = path[prefixLength..];
            Entries.Add(path, []);

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

            Elements.Insert(index + values.Length + 1, new qARKSpace());

            return this;
        }

        for (int i = 0; i < min; i++)
            valueEntries[i].Value = values[i]?.ToString() ?? string.Empty;

        if (moreValues)
        {
            var target = valueEntries.LastOrDefault();
            var index = Elements.IndexOf(target) + 1;

            for (int i = min; i < max; i++)
            {
                var entry = new qARKEntry(target.AbsolutePath, target.RelativePath, Parser.ConvertToString(values[i]))
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
            Elements.Remove(valueEntries[i]);

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
    #endregion
}
