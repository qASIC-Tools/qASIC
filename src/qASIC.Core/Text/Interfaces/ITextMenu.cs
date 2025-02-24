namespace qASIC.Text
{
    public interface ITextMenu
    {
        string GenerateMenu();

        object Confirm();
        bool Cancel();

        void Select();
        void Deselect();

        void Move(int delta);

        int Position { get; set; }
    }
}