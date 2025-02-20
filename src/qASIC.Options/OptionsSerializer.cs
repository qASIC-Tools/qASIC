using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using qASIC.qARK;

namespace qASIC.Options
{
    public class OptionsSerializer
    {
        public OptionsSerializer() : this($"{System.IO.Path.GetDirectoryName(Environment.ProcessPath)}/settings.txt") { }

        public OptionsSerializer(string path)
        {
            Path = path;

            OnSave = args =>
            {
                var serializer = new qARKSerializer();
                var doc = new qARKDocument();

                foreach (var item in args.list)
                    doc.AddEntry(item.Key, item.Value.value.ToString());

                return serializer.Serialize(doc);
            };

            OnLoad = args =>
            {
                var serializer = new qARKSerializer();
                var doc = serializer.Deserialize(args.txt);

                var dict = new Dictionary<string, object>();

                var items = doc
                    .Where(x => x is qARKEntry)
                    .Select(x => x as qARKEntry)
                    .GroupBy(x => x.Path)
                    .Where(x => args.list.ContainsKey(x.Key));

                foreach (var item in items)
                    dict.Add(item.Key, item.First().GetValue(args.list[item.Key].defaultValue.GetType()));

                return dict;
            };
        }

        public string Path { get; set; }

        public event Func<OptionsSaveArgs, string> OnSave;
        public event Func<OptionsLoadArgs, Dictionary<string, object>> OnLoad;

        public void Save(OptionsList list)
        {
            if (string.IsNullOrWhiteSpace(Path))
                return;

            var txt = OnSave(new OptionsSaveArgs()
            {
                list = list,
            });

            var directory = System.IO.Path.GetDirectoryName(Path);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (var writer = new StreamWriter(Path))
                writer.Write(txt);
        }

        public OptionsList Load(OptionsList list)
        {
            var loadedList = new OptionsList();

            if (string.IsNullOrWhiteSpace(Path) || !File.Exists(Path))
                return loadedList;

            using (var reader = new StreamReader(Path))
            {
                var txt = reader.ReadToEnd();
                var loadedItemList = OnLoad(new OptionsLoadArgs()
                {
                    list = list,
                    txt = txt,
                });

                foreach (var item in loadedItemList)
                    loadedList.Set(item.Key, item.Value);
            }

            return loadedList;
        }
    }
}