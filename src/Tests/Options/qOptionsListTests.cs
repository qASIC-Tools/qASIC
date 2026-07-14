using qASIC.Options;

namespace Tests.Options;

[UnitTest(nameof(qOptionsList))]
public class qOptionsListTests : UnitTestHolderBase
{
    [UnitTest]
    public void Constructor()
    {
        var list = new qOptionsList();
    }

    [UnitTest]
    public void Add()
    {
        var list = new qOptionsList();
        list.Add(new qOption<float>("test"));
        qAssert.IsTrue(list.TryGetOption("test", out var option) && option is qOption<float>);
    }

    [UnitTest]
    public void Array()
    {
        var list = new qOptionsList()
        {
            new qOption<float>("test"),
        };

        qAssert.IsTrue(list["test"] is qOption<float>);
    }

    [UnitTest]
    public void Remove()
    {
        var list = new qOptionsList()
        {
            new qOption<float>("test"),
        };

        list.Remove("test");
        qAssert.IsFalse(list.Contains("test"));
    }

    [UnitTest]
    public void OnOptionValuesChanged()
    {
        var list = new qOptionsList()
        {
            new qOption<float>("test"),
        };
        list.OnOptionValuesChanged += (options) =>
        {
            var list = options.ToList();
            qAssert.IsTrue(list.Count == 1);
            qAssert.IsTrue(list[0] is qOption<float>);
        };

        list["test"].Value = 1f;
    }

    [UnitTest]
    public void OnOptionValuesChangedMultiple()
    {
        var list = new qOptionsList()
        {
            new qOption<float>("test"),
            new qOption<float>("test2"),
        };
        list.OnOptionValuesChanged += (options) =>
        {
            var list = options.ToList();
            qAssert.IsTrue(list.Count == 2);
        };

        list.ApplyOtherMask([new ("test", 1f), new("test2", 1f)]);
    }

    [UnitTest]
    public void OnOptionsValuesChangedRemoved()
    {
        var option = new qOption<float>("test");
        var list = new qOptionsList()
        {
            option,
        };

        list.OnOptionValuesChanged += _ =>
        {
            throw new Exception();
        };

        list.Remove("test");
        option.Value = 1f;
    }

    [UnitTest]
    public void Contains()
    {
        var list = new qOptionsList()
        {
            new qOption<float>("test"),
        };

        qAssert.IsTrue(list.Contains("test"));
        qAssert.IsFalse(list.Contains("test2"));
    }

    [UnitTest]
    public void TryGetValue()
    {
        var list = new qOptionsList()
        {
            new qOption<float>("test"),
        };

        qAssert.IsTrue(list.TryGetOption("test", out var option) && option is qOption<float>);
        qAssert.IsFalse(list.TryGetOption("test2", out _));
    }

    [UnitTest]
    public void GetValue()
    {
        var list = new qOptionsList()
        {
            new qOption<float>("test"),
        };

        qAssert.IsTrue(list.GetOption("test") is qOption<float>);
        qAssert.IsTrue(list.GetOption("test2") is null);
    }

    [UnitTest]
    public void ApplyOtherMask()
    {
        var list = new qOptionsList()
        {
            new qOption<float>("test"),
        };

        list.ApplyOtherMask([new("test", 1f)]);
        qAssert.IsTrue(list["test"].Value is float f && f == 1f);
    }
}
