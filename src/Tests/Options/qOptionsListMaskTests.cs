using qASIC.Options;

namespace Tests.Options;

[UnitTest(nameof(qOptionsListMask))]
public class qOptionsListMaskTests : UnitTestHolderBase
{
    [UnitTest]
    public void Constructor()
    {
        var mask = new qOptionsListMask(new qOptionsList());
        qAssert.IsTrue(mask.Target is qOptionsList);
    }

    [UnitTest]
    public void AddMask()
    {
        var mask = new qOptionsListMask(new qOptionsList()
        {
            new qOption<float>("test")
        });
        mask.AddMask("test", 1f);
        qAssert.IsTrue(mask.GetOption("test") is qOptionMask { Value: 1f, OptionName: "test", Target: qOption<float> });
    }

    [UnitTest]
    public void RemoveMask()
    {
        var mask = new qOptionsListMask(new qOptionsList()
        {
            new qOption<float>("test")
        });
        mask.AddMask("test", 1f);
        mask.RemoveMask("test");
        qAssert.IsTrue(mask.GetOption("test") is qOption<float>);
    }

    [UnitTest]
    public void GetMasks()
    {
        var mask = new qOptionsListMask(new qOptionsList()
        {
            new qOption<float>("test"),
            new qOption<float>("test2")
        });
        mask.AddMask("test", 1f);
        var maskedValues = new List<qOptionMask>(mask.GetMasks());
        qAssert.IsTrue(maskedValues.Count == 1);
        qAssert.IsTrue(maskedValues[0] is qOptionMask { OptionName: "test" });
    }

    [UnitTest]
    public void Contains()
    {
        var mask = new qOptionsListMask(new qOptionsList()
        {
            new qOption<float>("test"),
        });
        qAssert.IsTrue(mask.Contains("test"));
        mask.AddMask("test", 1f);
        qAssert.IsTrue(mask.Contains("test"));
    }

    [UnitTest]
    public void GetOption()
    {
        var mask = new qOptionsListMask(new qOptionsList()
        {
            new qOption<float>("test"),
            new qOption<float>("test2"),
        });
        mask.AddMask("test", 1f);
        qAssert.IsTrue(mask.GetOption("test") is qOptionMask);
        qAssert.IsTrue(mask.GetOption("test2") is qOption<float>);
    }

    [UnitTest]
    public void TryGetOption()
    {
        var mask = new qOptionsListMask(new qOptionsList()
        {
            new qOption<float>("test"),
            new qOption<float>("test2"),
        });
        mask.AddMask("test", 1f);
        qAssert.IsTrue(mask.TryGetOption("test", out var test) && test is qOptionMask);
        qAssert.IsTrue(mask.TryGetOption("test2", out var test2) && test2 is qOption<float>);
        qAssert.IsFalse(mask.TryGetOption("test3", out _));
    }

    [UnitTest]
    public void ApplyMask()
    {
        var mask = new qOptionsListMask(new qOptionsList()
        {
            new qOption<float>("test"),
        });

        mask.AddMask("test", 1f);
        mask.ApplyMask();
        qAssert.IsTrue(mask.Target.GetOption("test").Value is float f && f == 1f);
        qAssert.IsFalse(mask.GetMasks().Any());
    }

    [UnitTest]
    public void ApplyOtherMask()
    {
        var mask = new qOptionsListMask(new qOptionsList()
        {
            new qOption<float>("test"),
            new qOption<float>("test2"),
            new qOption<float>("test3"),
        });
        mask.AddMask("test", 1f);
        mask.ApplyOtherMask([new("test", 2f), new("test2", 3f), new("test3", 4f)]);
        qAssert.IsTrue(mask.GetOption("test") is qOptionMask { Value: 2f });
        qAssert.IsTrue(mask.GetOption("test2") is qOptionMask { Value: 3f });
    }
}
