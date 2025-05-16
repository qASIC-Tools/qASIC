using qASIC.qARK;

namespace qASIC
{
    public interface IConfigurable
    {
        qARKDocument CreateConfig();
        void LoadConfig(qARKHolder data);
    }
}