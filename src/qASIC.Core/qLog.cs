using qASIC.Communication;
using System;
using System.Text;

namespace qASIC
{
    public enum LogType : byte
    {
        Application,
        User,
        Internal,
        Clear,
    }

    public class qLog : INetworkSerializable
    {
        public qLog() { }

        public qLog(DateTime time, string message) : this(time, message, qDebug.DEFAULT_COLOR_TAG) { }

        public qLog(DateTime time, string message, qColor color) : this(time, message, LogType.Application, color) { }
        public qLog(DateTime time, string message, string colorTag) : this(time, message, LogType.Application, colorTag) { }

        public qLog(DateTime time, string message, LogType logType, qColor color)
        {
            this.time = time;
            this.message = message;
            this.logType = logType;
            this.color = color;
            colorTag = null;
        }

        public qLog(DateTime time, string message, LogType logType, string colorTag)
        {
            this.time = time;
            this.message = message;
            this.logType = logType;
            this.colorTag = colorTag;
        }

        public DateTime time;
        public string message = string.Empty;
        public LogType logType = LogType.Application;
        public string colorTag = qDebug.DEFAULT_COLOR_TAG;
        public qColor color = qColor.White;

        public static qLog CreateNow(string message) =>
            new qLog(DateTime.Now, message);

        public static qLog CreateNow(string message, qColor color) =>
            new qLog(DateTime.Now, message, color);

        public static qLog CreateNow(string message, string colorTag) =>
            new qLog(DateTime.Now, message, colorTag);

        public static qLog CreateNow(string message, LogType logType, qColor color) =>
            new qLog(DateTime.Now, message, logType, color);

        public static qLog CreateNow(string message, LogType logType, string colorTag) =>
            new qLog(DateTime.Now, message, logType, colorTag);

        public override string ToString() =>
            $"[{time:HH:mm:ss}] [{logType}] {message}";

        /// <summary>Returns a string that represents the current object using a format.</summary>
        /// <param name="format">Format of the string.
        /// <list type="bullet">
        /// <item>%TIME% or %TIME:[format]% - represents <see cref="time"/>. Optional format will be used in <see cref="DateTime.ToString(string?)"/>.</item>
        /// <item>%MESSAGE% - represents <see cref="message"/>.</item>
        /// <item>%TYPE% or %TYPE:Application,User,Internal,Clear% - represents <see cref="logType"/>. Optionally you can specify text that will be used for every value.</item>
        /// <item>%TAG% - represents <see cref="colorTag"/>.</item>
        /// <item>%COLOR% - represents <see cref="color"/>.</item>
        /// <item>%% - represents the '%' character.</item>
        /// </list>
        /// </param>
        /// <returns>A string that represents the current object.</returns>
        public string ToString(string format)
        {
            var txt = new StringBuilder();
            var block = new StringBuilder();
            bool buildingBlock = false;

            for (int i = 0; i < format.Length; i++)
            {
                if (format[i] == '%')
                {
                    buildingBlock = !buildingBlock;

                    if (!buildingBlock)
                    {
                        var blockTxt = block.ToString();
                        var blockTxtLow = blockTxt.ToLower();
                        block.Clear();

                        //Values with formats
                        if (blockTxtLow.StartsWith("time:"))
                        {
                            txt.Append(time.ToString(blockTxt.Substring(5, blockTxt.Length - 5)));
                            continue;
                        }

                        if (blockTxtLow.StartsWith("type:"))
                        {
                            var parts = blockTxt.Substring(5, blockTxt.Length - 5).Split(',');
                            var index = logType switch
                            {
                                LogType.Application => 0,
                                LogType.User => 1,
                                LogType.Internal => 2,
                                LogType.Clear => 3,
                                _ => 4,
                            };

                            txt.Append(index < parts.Length ? parts[index] : "");
                            continue;
                        }

                        //Normal values
                        txt.Append(blockTxtLow switch
                        {
                            "time" => time.ToString(),
                            "message" => message,
                            "type" => logType,
                            "color" => color,
                            "" => "%",
                            _ => "",
                        });
                    }

                    continue;
                }

                if (buildingBlock)
                {
                    block.Append(format[i]);
                    continue;
                }

                txt.Append(format[i]);
            }

            return txt.ToString();
        }

        /// <summary>Changes message of the log.</summary>
        /// <param name="message">New log message.</param>
        /// <returns>Returns itself.</returns>
        public qLog ChangeMessage(string message)
        {
            this.message = message;
            return this;
        }

        /// <summary>Changes color of the log.</summary>
        /// <param name="color">New log color.</param>
        /// <returns>Returns itself.</returns>
        public qLog ChangeColor(qColor color)
        {
            this.color = color;
            colorTag = null;
            return this;
        }

        /// <summary>Changes color of the log.</summary>
        /// <param name="colorTag">New log color tag.</param>
        /// <returns>Returns itself.</returns>
        public qLog ChangeColor(string colorTag)
        {
            color = qColor.White;
            this.colorTag = colorTag;
            return this;
        }

        /// <summary>Copies data from a different log to itself.</summary>
        /// <param name="other">Log to copy data from.</param>
        /// <returns>Returns itself.</returns>
        public qLog GetDataFromOther(qLog other)
        {
            time = other.time;
            message = other.message;
            logType = other.logType;
            colorTag = other.colorTag;
            color = other.color;

            return this;
        }

        public qPacket Write(qPacket packet) =>
            packet
            .Write(time.Ticks)
            .Write(message)
            .Write((byte)logType)
            .Write(colorTag == null)
            .Write(colorTag ?? string.Empty)
            .Write(color);

        public void Read(qPacket packet)
        {
            time = new DateTime(packet.ReadLong());
            message = packet.ReadString();
            logType = (LogType)packet.ReadByte();

            bool nullColorTag = packet.ReadBool();
            colorTag = packet.ReadString();
            if (nullColorTag)
                colorTag = null;

            color = packet.ReadNetworkSerializable<qColor>();
        }
    }
}
