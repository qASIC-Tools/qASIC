using qASIC.Communication;
using System;
using System.Text;

namespace qASIC;

public enum LogType : byte
{
    Application,
    User,
    Internal,
    Clear,
}

public class qLog : INetworkSerializable
{
    public const string DEFAULT_TAG = "default";

    public qLog() { }

    public qLog(DateTime time, string message) : this(time, message, null) { }

    public qLog(DateTime time, string message, qColor color) : this(time, message, LogType.Application, color) { }
    public qLog(DateTime time, string message, string colorTag) : this(time, message, LogType.Application, colorTag) { }

    public qLog(DateTime time, string message, LogType logType, qColor color)
    {
        this.time = time;
        this.message = message;
        this.logType = logType;
        this.color = color;
        tag = null;
    }

    public qLog(DateTime time, string message, LogType logType, string tag)
    {
        this.time = time;
        this.message = message;
        this.logType = logType;
        this.tag = tag;
    }

    public DateTime time;
    public string message = string.Empty;
    public LogType logType = LogType.Application;
    public string tag = null;
    public qColor color = qColor.White;
    public bool sticky = false;

    public static qLog CreateNow(string message) =>
        new(DateTime.Now, message);

    public static qLog CreateNow(string message, qColor color) =>
        new(DateTime.Now, message, color);

    public static qLog CreateNow(string message, string colorTag) =>
        new(DateTime.Now, message, colorTag);

    public static qLog CreateNow(string message, LogType logType, qColor color) =>
        new(DateTime.Now, message, logType, color);

    public static qLog CreateNow(string message, LogType logType, string colorTag) =>
        new(DateTime.Now, message, logType, colorTag);

    public override string ToString() =>
        $"[{time:HH:mm:ss}] [{logType}] {message}";

    /// <summary>Returns a string that represents the current object using a format.</summary>
    /// <param name="format">Format of the string.
    /// <list type="bullet">
    /// <item>%TIME% or %TIME:[format]% - represents <see cref="time"/>. Optional format will be used in <see cref="DateTime.ToString(string?)"/>.</item>
    /// <item>%MESSAGE% - represents <see cref="message"/>.</item>
    /// <item>%TYPE% or %TYPE:Application,User,Internal,Clear% - represents <see cref="logType"/>. Optionally you can specify text that will be used for every value.</item>
    /// <item>%TAG% - represents <see cref="tag"/>.</item>
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
                        txt.Append(time.ToString(blockTxt[5..]));
                        continue;
                    }

                    if (blockTxtLow.StartsWith("type:"))
                    {
                        var parts = blockTxt[5..].Split(',');
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
        tag = null;
        return this;
    }

    /// <summary>Changes color of the log.</summary>
    /// <param name="tag">New log color tag.</param>
    /// <returns>Returns itself.</returns>
    public qLog ChangeColor(string tag)
    {
        color = qColor.White;
        this.tag = tag;
        return this;
    }

    /// <summary>Makes the log sticky.</summary>
    /// <returns>Returns itself.</returns>
    public qLog Sticky()
    {
        sticky = true;
        return this;
    }

    /// <summary>Stops the log from being sticky.</summary>
    /// <returns>Returns itself.</returns>
    public qLog UnStick()
    {
        sticky = false;
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
        tag = other.tag;
        color = other.color;

        return this;
    }

    public qPacket Write(qPacket packet) =>
        packet
        .Write(time.Ticks)
        .Write(message)
        .Write((byte)logType)
        .Write(tag == null)
        .Write(tag ?? string.Empty)
        .Write(color)
        .Write(sticky);

    public void Read(qPacket packet)
    {
        time = new DateTime(packet.ReadLong());
        message = packet.ReadString();
        logType = (LogType)packet.ReadByte();

        bool nullColorTag = packet.ReadBool();
        tag = packet.ReadString();
        if (nullColorTag)
            tag = null;

        color = packet.ReadNetworkSerializable<qColor>();
        sticky = packet.ReadBool();
    }
}
