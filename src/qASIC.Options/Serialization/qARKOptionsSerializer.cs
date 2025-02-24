using System;
using qASIC.qARK;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace qASIC.Options.Serialization
{
    public class qARKOptionsSerializer : OptionsSerializer
    {
        public qARKOptionsSerializer() : this($"{System.IO.Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)}/settings.txt") { }

        public qARKOptionsSerializer(string path)
        {
            Path = path;
        }

        public qARKSerializer Serializer { get; set; } = new qARKSerializer();

        public string Path { get; set; }

        private qARKDocument LoadDoc()
        {
            if (string.IsNullOrWhiteSpace(Path) || !File.Exists(Path))
                return new qARKDocument();

            var txt = File.ReadAllText(Path);
            return Serializer.Deserialize(txt);
        }

        public override void Save(OptionsList list)
        {
            if (string.IsNullOrWhiteSpace(Path))
                return;

            var doc = LoadDoc();
            foreach (var item in list)
            {
                if (item.Value.value is IEnumerable<object> enumerable)
                {
                    doc.SetValues(item.Key, enumerable.ToArray());
                    continue;
                }

                doc.SetValue(item.Key, item.Value.value.ToString());
            }

            var txt = Serializer.Serialize(doc);
            var directory = System.IO.Path.GetDirectoryName(Path);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (var writer = new StreamWriter(Path))
                writer.Write(txt);
        }

        public override OptionsList Load(OptionsList list)
        {
            var loadedList = new OptionsList();

            if (string.IsNullOrWhiteSpace(Path) || !File.Exists(Path))
                return loadedList;

            var doc = LoadDoc();

            foreach (var item in list)
            {
                if (item.Value == null) continue;
                var type = item.Value.value.GetType();
                
                if (type.IsArray)
                {
                    var itemType = type.GetElementType();
                    var result = doc.GetValueArray(itemType, item.Key)
                        .ToArray();

                    var array = Array.CreateInstance(itemType, result.Length);
                    Array.Copy(result, array, array.Length);

                    loadedList.Set(item.Key, array);
                    continue;
                }

                if (item.Value.value is IList && type.IsGenericType)
                {
                    var itemType = type.GetGenericArguments()[0];
                    var result = doc.GetValueArray(itemType, item.Key);

                    var valList = (IList)TypeFinder.CreateConstructorFromType(type);
                    foreach (var obj in result)
                        valList.Add(obj);

                    loadedList.Set(item.Key, valList);
                    continue;
                }

                if (item.Value.value is IEnumerable && type.IsGenericType)
                {
                    var itemType = type.GetGenericArguments()[0];
                    var result = doc.GetValueArray(itemType, item.Key);

                    loadedList.Set(item.Key, result.GetEnumerator());
                    continue;
                }

                if (doc.TryGetValue(type, item.Key, out var value))
                    loadedList.Set(item.Key, value);
            }

            return loadedList;
        }
    }
}