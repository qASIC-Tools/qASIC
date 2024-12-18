using qASIC.Core.QML;
using System.Collections.Generic;

namespace qASIC.QML
{
    public class QmlObject : QmlHolder
    {
        public QmlObject() : this(string.Empty, System.Array.Empty<QmlElement>()) { }
        public QmlObject(string path) : this(path, System.Array.Empty<QmlElement>()) { }
        public QmlObject(string path, IEnumerable<QmlElement> elements) : base(elements)
        {
            Path = path;
        }

        protected override string PathPrefix =>
            $"{Path}.";

        public string Path { get; set; }
    }
}