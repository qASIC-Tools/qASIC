namespace qASIC.Options
{
    public struct ChangeOptionArgs
    {
        public string optionName;
        public object value;
    }

    public struct OptionsLoadArgs
    {
        public OptionsList list;
        public string txt;
    }

    public struct OptionsSaveArgs
    {
        public OptionsList list;
    }
}