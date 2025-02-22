namespace qASIC.Options.Serialization
{
    public abstract class OptionsSerializer
    {
        public abstract void Save(OptionsList list);

        public abstract OptionsList Load(OptionsList list);
    }
}