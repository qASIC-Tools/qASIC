using System.Threading.Tasks;

namespace qASIC.Console.Commands
{
    public abstract class GameCommandAsync : GameCommand
    {
        public override object Run(GameCommandContext context) =>
            RunAsync(context);

        public abstract Task<object> RunAsync(GameCommandContext context);
    }
}