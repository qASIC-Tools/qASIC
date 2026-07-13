using System.Collections.Generic;
using System.Threading.Tasks;

namespace qASIC.Options;

public interface IOptionsSaveManager
{
    void Save(IOptionsList list, IEnumerable<IOption> options);
    Task SaveAsync(IOptionsList list, IEnumerable<IOption> options);

    void Load(IOptionsList list);
    Task LoadAsync(IOptionsList list);
}
