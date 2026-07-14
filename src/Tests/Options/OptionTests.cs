using qASIC.Options;

namespace Tests.Options;

[UnitTest("Option")]
public class OptionTest : UnitTestHolderBase
{
    [UnitTest]
    public void Constructor()
    {
        var option = new qOption<float>("test", 0f);
        qAssert.IsTrue(option is qOption { OptionName: "test", DefaultValue: 0f, Value: 0f });
    }

    [UnitTest]
    public void Value()
    {
        var option = new qOption<float>("test", 0f)
        {
            Value = 1f
        };
        qAssert.IsTrue(option is qOption { DefaultValue: 0f, Value: 1f });
    }

    [UnitTest]
    public void OnValueChanged()
    {
        var option = new qOption<float>("test", 0f);
        option.OnValueChanged += opt => qAssert.IsTrue(opt is qOption { OptionName: "test", Value: 1f });
        option.Value = 1f;
    }
}
