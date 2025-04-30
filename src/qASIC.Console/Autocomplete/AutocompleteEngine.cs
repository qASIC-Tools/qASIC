namespace qASIC.Console.Autocomplete
{
    public abstract class AutocompleteEngine
    {
        public AutocompleteEngine(qConsole console)
        {
            Console = console;
        }

        public qConsole Console { get; set; }

        public abstract (string, int) Autocomplete(string cmd, int cursorPosition);
    }
}