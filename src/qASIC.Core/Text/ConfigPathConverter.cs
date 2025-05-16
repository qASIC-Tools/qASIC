using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace qASIC.Text
{
    public class ConfigPathConverter
    {
        public ConfigPathConverter() : this(new Dictionary<string, Func<string, string>>()) { }

        public ConfigPathConverter(Dictionary<string, Func<string, string>> tags)
        {
            Tags = new Dictionary<string, Func<string, string>>(tags);
        }

        public static ConfigPathConverter CreateStandardConverter() =>
            new ConfigPathConverter(new Dictionary<string, Func<string, string>>()
            {
                ["DESKTOP"] = args => Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                ["DOCUMENTS"] = args => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                ["FAVOURITES"] = args => Environment.GetFolderPath(Environment.SpecialFolder.Favorites),
                ["MUSIC"] = args => Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                ["VIDEOS"] = args => Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                ["APPDATA"] = args => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ["LOCALAPPDATA"] = args => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ["PICTURES"] = args => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                ["USER"] = args => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ["APP"] = args => AppDomain.CurrentDomain.BaseDirectory,
                ["DATE"] = args => DateTime.Now.ToString(args),
                ["DATEUTC"] = args => DateTime.UtcNow.ToString(args),
            });

        /// <summary>Dictionary of tags that will be replaced by the result of the method.
        /// For example
        /// <code>Converter.Tags.Add("DATE", args => DateTime.Now.ToString(args));</code>
        /// will be used for
        /// <code>%DATE:yyyy:MM:dd%</code>
        /// </summary>
        public Dictionary<string, Func<string, string>> Tags { get; private set; }

        public string ConvertToFinal(string configPath)
        {
            var txt = new StringBuilder();
            var queue = new Queue<char>(configPath);

            while (queue.TryDequeue(out var c))
            {
                //CASE: normal letter
                if (c != '%')
                {
                    txt.Append(c);
                    continue;
                }

                //CASE: tag
                //Getting the whole tag
                var tagTxt = new StringBuilder();
                char nextC;
                while (queue.TryDequeue(out nextC) && nextC != '%')
                    tagTxt.Append(nextC);

                //if the tag wasn't closed or if it was just '%%'
                //treat the tag as normal text and '%%' as just a single '%'
                if (nextC !='%' || tagTxt.Length == 0)
                {
                    txt.Append('%');
                    txt.Append(tagTxt);
                    continue;
                }

                var tagVal = tagTxt.ToString();
                var tag = tagVal.Split(':').First();
                var tagArgs = string.Join(':', tagVal.Split(':').Skip(1));

                //If the tag doesn't exist, treat it as normal text
                if (!Tags.TryGetValue(tag, out var path))
                {
                    txt.Append('%');
                    txt.Append(tagVal);
                    txt.Append('%');
                    continue;
                }

                txt.Append(path.Invoke(tagArgs));
            }

            return Path.GetFullPath(txt.ToString());
        }
    }
}