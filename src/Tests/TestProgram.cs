namespace Tests;

using Tests.Console;
using SysConsole = System.Console;

public class TestProgram
{
    public QuashParserTests QuashTester { get; } = new();

    public void Run()
    {
        while (true)
        {
            SysConsole.WriteLine("[1] Test QuashParse");
            SysConsole.WriteLine("[2] Exit");
            SysConsole.WriteLine();
            SysConsole.Write("Select option: ");

            var key = SysConsole.ReadKey();
            SysConsole.WriteLine();
        
            switch (key.KeyChar)
            {
                case '1':
                    QuashTester.ParseVisually(QuashParserTests.TEST_SCRIPT);
                    break;
                case '2':
                    return;
                default:
                    SysConsole.WriteLine("Unknown option");
                    break;
            }

            SysConsole.WriteLine("----------------------------------------");
            SysConsole.WriteLine();
        }
    }
}