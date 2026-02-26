using System.Diagnostics;
using System.Reflection;
using qASIC.Logging;

namespace Tests;

public class UnitTestHolderBase : IHasLogs
{
    public qLogManager Logs { get; } = new();

    Dictionary<string, MethodInfo> _testMethods = [];

    public IEnumerable<string> TestNames => _testMethods.Keys;

    public void PrepareTestsList()
    {
        var ownAttr = GetType().GetCustomAttribute<UnitTestAttribute>();
        var prefix = ownAttr == null ? string.Empty : $"{(string.IsNullOrWhiteSpace(ownAttr.Name) ? GetType().Name : ownAttr.Name)}.";

        foreach (var item in GetType().GetMethods().Where(x => x.GetParameters().Length == 0))
        {
            var attr = item.GetCustomAttribute<UnitTestAttribute>();
            if (attr == null) continue;
            var name = string.IsNullOrWhiteSpace(attr.Name) ? item.Name : attr.Name;
            _testMethods.Add($"{prefix}{name}", item);
        }
    }

    public bool RunTest(string testName)
    {
        var method = _testMethods[testName];
        try
        {
            method.Invoke(this, []);
            return true;
        }
        catch (Exception e)
        {
            Logs.LogError($"Test {testName} failed: {e}");
            return false;
        }
    }
}