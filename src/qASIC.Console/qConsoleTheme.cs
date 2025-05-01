using qASIC.Communication;
using qASIC.qARK;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace qASIC.Console
{
    public class qConsoleTheme : INetworkSerializable
    {
        public static qConsoleTheme Default =>
            new qConsoleTheme();

        public qColor defaultColor = qColor.White;
        public qColor warningColor = qColor.Yellow;
        public qColor errorColor = qColor.Red;

        public Dictionary<string, qColor> customColors = new Dictionary<string, qColor>()
        {
            ["settings"] = new qColor(0, 0, 255),
            ["settings_set"] = new qColor(0, 0, 255),
            ["settings_set_multiple"] = new qColor(0, 0, 255),
            ["settings_ensure_targets"] = new qColor(0, 0, 255),
            ["settings_init"] = new qColor(0, 0, 255),
            ["settings_save_success"] = new qColor(0, 0, 255),
            ["settings_load_success"] = new qColor(0, 0, 255),
        };

        public qColor this[string s]
        {
            get
            {
                switch (s)
                {
                    case qDebug.DEFAULT_TAG:
                        return defaultColor;
                    case qDebug.WARNING_TAG:
                        return warningColor;
                    case qDebug.ERROR_TAG:
                        return errorColor;
                    default:
                        return customColors.TryGetValue(s, out var cl) ? cl : defaultColor;
                }
            }
            set
            {
                if (customColors.ContainsKey(s))
                {
                    customColors[s] = value;
                    return;
                }

                customColors.Add(s, value);
            }
        }

        public qColor GetLogColor(qLog log)
        {
            if (log.tag == null)
                return log.color;

            return this[log.tag];
        }

        public void Read(qPacket packet)
        {
            customColors.Clear();
            int colorCount = packet.ReadInt();
            for (int i = 0; i < colorCount; i++)
                customColors.SetOrAdd(packet.ReadString(), packet.ReadNetworkSerializable<qColor>());

            defaultColor = packet.ReadNetworkSerializable<qColor>();
            warningColor = packet.ReadNetworkSerializable<qColor>();
            errorColor = packet.ReadNetworkSerializable<qColor>();
        }

        public qPacket Write(qPacket packet)
        {
            packet = packet
                .Write(customColors.Count);

            foreach (var item in customColors)
            {
                packet.Write(item.Key);
                packet.Write(item.Value);
            }

            packet = packet.Write(defaultColor)
                .Write(warningColor)
                .Write(errorColor);

            return packet;
        }

        public void LoadConfiguration(qARKHolder holder)
        {
            defaultColor = holder.GetValue(qDebug.DEFAULT_TAG, defaultColor);
            warningColor = holder.GetValue(qDebug.WARNING_TAG, warningColor);
            errorColor = holder.GetValue(qDebug.ERROR_TAG, errorColor);

            customColors.Clear();
            foreach (var item in holder)
            {
                if (item is qARKEntry entry)
                {
                    if (entry.Path == qDebug.DEFAULT_TAG ||
                        entry.Path == qDebug.WARNING_TAG ||
                        entry.Path == qDebug.ERROR_TAG)
                        continue;

                    if (entry.TryGetValue(out qColor color))
                        customColors.SetOrAdd(entry.Path, color);
                }
            }
        }

        public static qARKDocument CreateConfiguration(qARKHolder original)
        {
            var doc = new qARKDocument()
                .AddEntry(qDebug.DEFAULT_TAG, original.GetValue(qDebug.DEFAULT_TAG, qColor.White))
                .AddEntry(qDebug.WARNING_TAG, original.GetValue(qDebug.WARNING_TAG, qColor.Yellow))
                .AddEntry(qDebug.ERROR_TAG, original.GetValue(qDebug.ERROR_TAG, qColor.Red));

            foreach (var item in original)
            {
                if (item is qARKEntry entry)
                {
                    if (entry.Path == qDebug.DEFAULT_TAG ||
                        entry.Path == qDebug.WARNING_TAG ||
                        entry.Path == qDebug.ERROR_TAG)
                        continue;

                    doc.AddEntry(entry.Path, entry.Value);
                }
            }

            return doc;
        }
    }
}