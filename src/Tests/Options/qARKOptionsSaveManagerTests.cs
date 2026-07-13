using qASIC.Options;

namespace Tests.Options;

[UnitTest("qARKOptionsSaveManager")]
public class qARKOptionsSaveManagerTests : UnitTestHolderBase
{
    [UnitTest]
    public void DeserializeSingleValues()
    {
        var txt = @"
test = 1
test2 = 3
";

        var manager = new TestSaveManager();
        var list = new OptionsList
        {
            new Option<float>("test", 0f),
            new Option<float>("test3", 0f),
        };
        
        manager.DeserializePublic(list, txt);
        qAssert.IsTrue(list.GetOption("test") is Option<float> { Value: 1f });
        qAssert.IsFalse(list.TryGetOption("test2", out _));
        qAssert.IsTrue(list.GetOption("test3") is Option<float> { Value: 0f });
    }

    [UnitTest]
    public void DeserializeArray()
    {
        var txt = @"
test|
* value1
* value2
";

        var manager = new TestSaveManager();
        var list = new OptionsList
        {
            new Option<string[]>("test", []),
        };

        manager.DeserializePublic(list, txt);
        qAssert.IsTrue(list.GetOption("test").Value is string[]);
        qAssert.CompareCollections((string[])list.GetOption("test").Value, ["value1", "value2"]);
    }

    [UnitTest]
    public void DeserializeList()
    {
        var txt = @"
test|
* value1
* value2
";

        var manager = new TestSaveManager();
        var list = new OptionsList()
        {
            new Option<List<string>>("test", []),
        };

        manager.DeserializePublic(list, txt);
        qAssert.IsTrue(list.GetOption("test").Value is List<string>);
        qAssert.CompareCollections((List<string>)list.GetOption("test").Value, ["value1", "value2"]);
    }

    [UnitTest]
    public void DeserializeWrong()
    {
        var txt = @"
test = false
test2 = false
";

        var manager = new TestSaveManager();
        var list = new OptionsList()
        {
            new Option<float>("test", 3f),
        };

        manager.DeserializePublic(list, txt);
        qAssert.IsTrue(list.Count() == 1);
        qAssert.IsTrue(list.GetOption("test") is Option<float> { Value: 3f });
    }

    private class TestSaveManager : qARKOptionsSaveManager
    {
        public void SerializePublic(string baseText, IOptionsList list, IEnumerable<IOption> options) =>
            Serialize(baseText, list, options);
        public void DeserializePublic(IOptionsList list, string txt) =>
            Deserialize(list, txt);
    }
}
