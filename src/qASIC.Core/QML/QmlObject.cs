using qASIC.Core.QML;
using System.Collections.Generic;

namespace qASIC.QML
{
    public class QmlObject : QmlHolder
    {
        public QmlObject() : this(string.Empty, System.Array.Empty<QmlElement>()) { }
        public QmlObject(string path) : this(string.Empty, System.Array.Empty<QmlElement>()) { }
        public QmlObject(string path, IEnumerable<QmlElement> elements) : base(elements)
        {
            Path = path;
        }

        public string Path { get; set; }
    }
}