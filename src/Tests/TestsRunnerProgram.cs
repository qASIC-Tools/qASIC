namespace Tests;

using System.Reflection;
using System.Text;
using qASIC;
using SysConsole = System.Console;

public class TestsRunnerProgram
{
    public List<UnitTestHolderBase> holders = [];

    public void Run()
    {
        FindTestHolders();
        while (true)
        {
            SysConsole.ForegroundColor = ConsoleColor.Cyan;
            SysConsole.WriteLine("Select an option");
            SysConsole.ForegroundColor = ConsoleColor.DarkGray;
            SysConsole.Write("[1] ");
            SysConsole.ResetColor();
            SysConsole.WriteLine("Run all tests");
            SysConsole.ForegroundColor = ConsoleColor.DarkGray;
            SysConsole.Write("[2] ");
            SysConsole.ResetColor();
            SysConsole.WriteLine("Exit");

            var key = SysConsole.ReadKey();
            switch (key.KeyChar)
            {
                case '1':
                    RunTests(holders);
                    break;
                case '2':
                    return;
            }
        }
    }

    public void RunTests(IEnumerable<UnitTestHolderBase> holders)
    {
        SysConsole.WriteLine("Running tests...");

        var logsBuffer = new StringBuilder();

        var count = holders.Sum(x => x.TestNames.Count());
        var index = 0;
        var successes = 0;

        foreach (var holder in holders)
        {
            holder.Logs.OnLog += WriteToBuffer;
            foreach (var test in holder.TestNames)
            {
                SysConsole.ForegroundColor = ConsoleColor.DarkGray;
                SysConsole.Write($"[{index+1}/{count}] ");
                SysConsole.ResetColor();
                SysConsole.Write($"Starting test '{test}': ");
                
                var success = holder.RunTest(test); 
                if (success) successes++;

                SysConsole.ForegroundColor = success ? ConsoleColor.Green : ConsoleColor.Red;
                SysConsole.WriteLine(success ? "Success" : "Fail");
                SysConsole.ResetColor();

                SysConsole.Write(logsBuffer.ToString());
                logsBuffer.Clear();

                index++;
            }

            holder.Logs.OnLog -= WriteToBuffer;
        }

        SysConsole.WriteLine();
        SysConsole.ForegroundColor = ConsoleColor.Cyan;
        SysConsole.WriteLine("ALL TESTS FINISHED");
        SysConsole.ResetColor();
        SysConsole.WriteLine($"{successes} succeeded, {count - successes} failed");
        SysConsole.WriteLine();


        void WriteToBuffer(qLog log) =>
            logsBuffer.AppendLine(log.message);
    }

    public void FindTestHolders()
    {
        holders.Clear();
        var baseType = typeof(UnitTestHolderBase);

        foreach (var item in Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x.IsAssignableTo(baseType) && x != baseType))
        {
            var holder = (UnitTestHolderBase)Activator.CreateInstance(item);
            holders.Add(holder);
            holder.PrepareTestsList();
        }
    }
}