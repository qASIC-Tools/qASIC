using qASIC.Options;

namespace Tests.Options;

[UnitTest("OptionMask")]
public class OptionMaskTests : UnitTestHolderBase
{
    [UnitTest]
    public void Constructor1()
    {
        var option = new qOption<float>("test", 0f);
        var mask = new qOptionMask(option, 1f);
        qAssert.IsTrue(mask is qOptionMask { OptionName: "test", DefaultValue: 0f, Value: 1f, Target: qOption { Value: 0f } });
    }

    [UnitTest]
    public void Constructor2()
    {
        var option = new qOption<float>("test", 0f, 1f);
        var mask = new qOptionMask(option);
        qAssert.IsTrue(mask is qOptionMask { OptionName: "test", DefaultValue: 0f, Value: 1f});
    }

    [UnitTest]
    public void Value()
    {
        var option = new qOption<float>("test", 0f);
        var mask = new qOptionMask(option)
        {
            Value = 1f,
        };
        qAssert.IsTrue(mask is qOptionMask { Value: 1f, Target: qOption { Value: 0f } });
    }

    [UnitTest]
    public void OnValueChanged()
    {
        var option = new qOption<float>("test", 0f);
        var mask = new qOptionMask(option);
        mask.OnValueChanged += x => qAssert.IsTrue(x is qOptionMask { OptionName: "test", Value: 1f, Target: qOption { OptionName: "test", Value: 0f } });
        mask.Value = 1f;
    }

    [UnitTest]
    public void Apply()
    {
        var option = new qOption<float>("test", 0f);
        var mask = new qOptionMask(option, 1f);
        mask.Apply();
        qAssert.IsTrue(option is qOption { Value: 1f, DefaultValue: 0f });
    }

    [UnitTest]
    public void OnApply()
    {
        var option = new qOption<float>("test", 0f);
        var mask = new qOptionMask(option, 1f);
        mask.OnApply += opt => qAssert.IsTrue(opt is qOptionMask { Target: qOption { Value: 1f } });
        mask.Apply();
    }
}
