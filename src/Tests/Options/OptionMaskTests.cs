using qASIC.Options;

namespace Tests.Options;

[UnitTest("OptionMask")]
public class OptionMaskTests : UnitTestHolderBase
{
    [UnitTest]
    public void Constructor1()
    {
        var option = new Option<float>("test", 0f);
        var mask = new OptionMask(option, 1f);
        qAssert.IsTrue(mask is OptionMask { OptionName: "test", DefaultValue: 0f, Value: 1f, Target: Option { Value: 0f } });
    }

    [UnitTest]
    public void Constructor2()
    {
        var option = new Option<float>("test", 0f, 1f);
        var mask = new OptionMask(option);
        qAssert.IsTrue(mask is OptionMask { OptionName: "test", DefaultValue: 0f, Value: 1f});
    }

    [UnitTest]
    public void Value()
    {
        var option = new Option<float>("test", 0f);
        var mask = new OptionMask(option)
        {
            Value = 1f,
        };
        qAssert.IsTrue(mask is OptionMask { Value: 1f, Target: Option { Value: 0f } });
    }

    [UnitTest]
    public void OnValueChanged()
    {
        var option = new Option<float>("test", 0f);
        var mask = new OptionMask(option);
        mask.OnValueChanged += x => qAssert.IsTrue(x is OptionMask { OptionName: "test", Value: 1f, Target: Option { OptionName: "test", Value: 0f } });
        mask.Value = 1f;
    }

    [UnitTest]
    public void Apply()
    {
        var option = new Option<float>("test", 0f);
        var mask = new OptionMask(option, 1f);
        mask.Apply();
        qAssert.IsTrue(option is Option { Value: 1f, DefaultValue: 0f });
    }

    [UnitTest]
    public void OnApply()
    {
        var option = new Option<float>("test", 0f);
        var mask = new OptionMask(option, 1f);
        mask.OnApply += opt => qAssert.IsTrue(opt is OptionMask { Target: Option { Value: 1f } });
        mask.Apply();
    }
}
