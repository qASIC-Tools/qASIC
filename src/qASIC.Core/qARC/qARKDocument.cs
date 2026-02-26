using qASIC.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace qASIC.qARK;

/// <summary>A document containing qARK entries, used for serialization.</summary>
public class qARKDocument : qARKHolder
{
    public const string FILE_EXTENSION = "qark";

    public qARKDocument() : this(new(), []) { }
    public qARKDocument(ModularParser parser) : this(parser, []) { }
    public qARKDocument(IEnumerable<qARKElement> elements) : this(new(), elements) { }
    public qARKDocument(ModularParser parser, IEnumerable<qARKElement> elements) : base()
    {
        Parser = parser;
        foreach (var item in elements)
            Add(item);
    }

    public string NewElementPathPrefix { get; private set; } = "";

    #region Adding
    protected override qARKElement PrepareElementForHolder(qARKElement element, int index)
    {
        switch (element)
        {
            case qARKEntry entry:
                HandleEntry(entry);
                break;
            case qARKGroupBorder group:
                HandleGroup(group);
                break;
        }

        return base.PrepareElementForHolder(element, index);


        void HandleGroup(qARKGroupBorder group)
        {
            var thisGroupPrefix = string.IsNullOrWhiteSpace(group.AbsolutePath) ?
                string.Empty :
                $"{group.AbsolutePath}.";

            var lastGroup = GetLastElementOfType<qARKGroupBorder>(index);

            // If this is going to be the last group, change element path prefix
            if (Elements.IndexOf(lastGroup) < index)
                NewElementPathPrefix = thisGroupPrefix;
            
            // Update absolute paths of proceeding entries
            for (int i = index; i < Elements.Count; i++)
            {
                switch (Elements[i])
                {
                    case qARKEntry entr:
                        var prevAbs = entr.AbsolutePath;
                        entr.AbsolutePath = $"{thisGroupPrefix}{entr.RelativePath}";
                        UpdateCachedEntry(entr, prevAbs);
                        break;
                    case qARKGroupBorder:
                        // Finish
                        return;
                }
            }
        }

        void HandleEntry(qARKEntry entry)
        {
            // Additional logic for array item entry
            if (entry.IsArrayItem)
            {
                entry.RelativePath = string.Empty;
                var prevEntry = GetLastElementOfType<qARKEntry>(index);

                if (prevEntry != null)
                {
                    // If previous entry's path starts differently than
                    // the non-relative path of this element, reset
                    // the path prefix by closing the group
                    if (!prevEntry.AbsolutePath.StartsWith(NewElementPathPrefix))
                        AddElement(new qARKGroupBorder());

                    // Copy relative path from previous entry
                    entry.RelativePath = prevEntry.AbsolutePath[NewElementPathPrefix.Length..];
                }
            }

            // For any type of entry
            entry.AbsolutePath = $"{NewElementPathPrefix}{entry.RelativePath}";
            entry.Parser = Parser;
        }
    }

    public qARKDocument AddElement(qARKElement element)
    {
        Add(element);
        return this;
    }

    public qARKDocument AddEntry(string path, object value) =>
        AddElement(new qARKEntry(path, Parser.ConvertToString(value)));

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
        AddElement(new qARKEntry(string.Empty, Parser.ConvertToString(value))
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
            }
        }
        return this;
    }
    #endregion

    #region Setting Values
    public qARKDocument SetValue(string path, object value)
    {
        var entry = GetEntry(path);
        if (entry == null)
        {
            // If the path under the current group would be incorrect, finish group
            if (!path.StartsWith(NewElementPathPrefix))
            {
                AddGroupEnd();
                AddSpace();
            }

            AddEntry(path[NewElementPathPrefix.Length..], value);
            return this;
        }

        entry.Value = value?.ToString() ?? string.Empty;
        return this;
    }

    public qARKDocument SetValues(string path, params object[] values)
    {
        var entries = GetEntries(path, includeWithoutValue: true);

        // If there are no existing entries
        if (entries.Length == 0)
        {
            // Ensure path will be correct
            if (!path.StartsWith(NewElementPathPrefix))
            {
                AddGroupEnd();
                AddSpace();
            }

            // Add entries
            AddArrayEntryFromValues(path[NewElementPathPrefix.Length..], values);
            return this;
        }

        var valueEntries = entries.Where(x => !x.IsArrayStart).ToArray();
        FillExistingValues();
        AppendNewValues();
        RemoveAdditionalValues();

        return this;


        void FillExistingValues()
        {
            var length = Math.Min(values.Length, valueEntries.Length);
            for (int i = 0; i < length; i++)
                valueEntries[i].Value = Parser.ConvertToString(values[i]);
        }

        void AppendNewValues()
        {
            var last = entries.Last();
            var index = Elements.IndexOf(last);
            for (int i = valueEntries.Length; i < values.Length; i++)
            {
                index++;
                Insert(index, new qARKEntry(last.RelativePath, Parser.ConvertToString(index)) { IsArrayItem = true, });
            }
        }

        void RemoveAdditionalValues()
        {
            for (int i = values.Length; i < valueEntries.Length; i++)
                Remove(valueEntries[i]);
        }
    }
    #endregion
}
