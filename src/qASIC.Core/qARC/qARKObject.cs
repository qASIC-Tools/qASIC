using System.Collections.Generic;

namespace qASIC.qARK;

public class qARKObject : qARKHolder
{
    public qARKObject() : this(string.Empty, []) { }
    public qARKObject(string path) : this(path, []) { }
    public qARKObject(string path, IEnumerable<qARKElement> elements) : base(elements)
    {
        Path = path;
    }

    protected override string PathPrefix =>
        $"{Path}.";

    public string Path { get; set; }
}
