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
            SysConsole.WriteLine("[1] Test Quash Parsing");
            SysConsole.WriteLine("[2] Test Quash Character Info");
            SysConsole.WriteLine("[3] Exit");
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
                    QuashTester.TestCharacterInfo();
                    break;
                case '3':
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