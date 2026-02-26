using System.Diagnostics;
using qASIC.qARK;

namespace Tests.Core;

[UnitTest("qARK")]
public class qARKTests : UnitTestHolderBase
{
    public qARKSerializer Serializer { get; } = new();

    [UnitTest]
    public void AddEntry()
    {
        var doc = new qARKDocument()
            .AddEntry("path", "value")
            .ToList();
        
        qAssert.IsTrue(doc[0] is qARKEntry { AbsolutePath: "path", RelativePath: "path", Value: "value" });
    }

    [UnitTest]
    public void AddGroupStart()
    {
        var doc = new qARKDocument()
            .AddGroupStart("group")
            .AddEntry("entry", "value")
            .ToList();

        qAssert.IsTrue(doc[0] is qARKGroupBorder { RelativePath: "group", AbsolutePath: "group" });
        qAssert.IsTrue(doc[1] is qARKEntry { RelativePath: "entry", AbsolutePath: "group.entry" });
    }

    [UnitTest]
    public void AddGroupEnd()
    {
        var doc = new qARKDocument()
            .AddGroupStart("group")
            .AddEntry("entry", "value")
            .AddGroupEnd()
            .AddEntry("entry", "value")
            .ToList();

        qAssert.IsTrue(doc[0] is qARKGroupBorder { AbsolutePath: "group", RelativePath: "group" });
        qAssert.IsTrue(doc[1] is qARKEntry { AbsolutePath: "group.entry", RelativePath: "entry" });
        qAssert.IsTrue(doc[2] is qARKGroupBorder { AbsolutePath: "", RelativePath: "" });
        qAssert.IsTrue(doc[3] is qARKEntry { AbsolutePath: "entry", RelativePath: "entry" });
    }

    [UnitTest]
    public void AddComment()
    {
        var doc = new qARKDocument()
            .AddComment("comment")
            .ToList();

        qAssert.IsTrue(doc[0] is qARKComment { Comment: "comment" });
    }

    [UnitTest]
    public void SetValues1()
    {
        var doc = new qARKDocument()
            .SetValues("value", 1, 2, 3)
            .ToList();

        qAssert.IsTrue(doc.Count == 4);
        qAssert.IsTrue(doc[0] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayStart: true });
        qAssert.IsTrue(doc[1] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayItem: true, Value: "1" });
        qAssert.IsTrue(doc[2] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayItem: true, Value: "2" });
        qAssert.IsTrue(doc[3] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayItem: true, Value: "3" });
    }

    [UnitTest]
    public void SetValues2()
    {
        var doc = new qARKDocument()
            .SetValues("value", 1, 2, 3)
            .SetValues("value", 1, 2, 3, 4)
            .ToList();
        
        qAssert.IsTrue(doc.Count == 5);
        qAssert.IsTrue(doc[0] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayStart: true });
        qAssert.IsTrue(doc[1] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayItem: true, Value: "1" });
        qAssert.IsTrue(doc[2] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayItem: true, Value: "2" });
        qAssert.IsTrue(doc[3] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayItem: true, Value: "3" });
        qAssert.IsTrue(doc[4] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayItem: true, Value: "4" });
    }

    [UnitTest]
    public void SetValues3()
    {
        var doc = new qARKDocument()
            .SetValues("value", 1, 2, 3)
            .SetValues("value", 1, 2)
            .ToList();

        qAssert.IsTrue(doc.Count == 3);
        qAssert.IsTrue(doc[0] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayStart: true });
        qAssert.IsTrue(doc[1] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayItem: true, Value: "1" });
        qAssert.IsTrue(doc[2] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayItem: true, Value: "2" });
    }

    [UnitTest]
    public void SetValues4()
    {
        var doc = new qARKDocument()
            .AddGroupStart("group")
            .SetValues("group.value", 1)
            .ToList();
        
        qAssert.IsTrue(doc.Count == 3);
        qAssert.IsTrue(doc[0] is qARKGroupBorder { RelativePath: "group", AbsolutePath: "group" });
        qAssert.IsTrue(doc[1] is qARKEntry { RelativePath: "value", AbsolutePath: "group.value", IsArrayStart: true });
        qAssert.IsTrue(doc[2] is qARKEntry { RelativePath: "value", AbsolutePath: "group.value", IsArrayItem: true, Value: "1" });
    }

    [UnitTest]
    public void SetValues5()
    {
        var doc = new qARKDocument()
            .AddGroupStart("group")
            .SetValues("value", 1)
            .ToList();
        
        qAssert.IsTrue(doc.Count == 5);
        qAssert.IsTrue(doc[0] is qARKGroupBorder { RelativePath: "group", AbsolutePath: "group" });
        qAssert.IsTrue(doc[1] is qARKGroupBorder { RelativePath: "", AbsolutePath: "" });
        qAssert.IsTrue(doc[2] is qARKSpace { Count: 1 });
        qAssert.IsTrue(doc[3] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayStart: true });
        qAssert.IsTrue(doc[4] is qARKEntry { RelativePath: "value", AbsolutePath: "value", IsArrayItem: true, Value: "1" });
    }

    [UnitTest]
    public void InsertGroup()
    {
        var doc = new qARKDocument()
            .AddGroupStart("group")
            .AddEntry("value", 1)
            .AddEntry("value", 2);
        
        doc.Insert(2, new qARKGroupBorder());

        var list = doc.ToList();
        
        qAssert.IsTrue(list.Count == 4);
        qAssert.IsTrue(list[0] is qARKGroupBorder { RelativePath: "group", AbsolutePath: "group" });
        qAssert.IsTrue(list[1] is qARKEntry { RelativePath: "value", AbsolutePath: "group.value", Value: "1" });
        qAssert.IsTrue(list[2] is qARKGroupBorder { RelativePath: "", AbsolutePath: "" });
        qAssert.IsTrue(list[3] is qARKEntry { RelativePath: "value", AbsolutePath: "value", Value: "2" });
    }
}