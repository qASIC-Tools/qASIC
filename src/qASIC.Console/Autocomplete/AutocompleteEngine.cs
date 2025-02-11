namespace qASIC.Console.Autocomplete
{
    public abstract class AutocompleteEngine
    {
        public AutocompleteEngine(GameConsole console)
        {
            Console = console;
        }

        public GameConsole Console { get; set; }

        public abstract (string, int) Autocomplete(string cmd, int cursorPosition);
    }
}