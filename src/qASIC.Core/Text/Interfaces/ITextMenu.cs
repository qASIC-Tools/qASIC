namespace qASIC.Text
{
    public interface ITextMenu
    {
        string GenerateMenu();

        object Confirm();
        bool Cancel();

        void Select();
        void Deselect();

        bool TryInvokeItemAction(char key, out object result);

        void Move(int delta);

        int Position { get; set; }
    }
}