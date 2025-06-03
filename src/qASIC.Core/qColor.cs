using qASIC.Communication;
using System;
using System.Collections.Generic;

using SysColor = System.Drawing.Color;

namespace qASIC
{
    public enum GenericColor
    {
        Clear,
        Black,
        White,
        Red,
        Green,
        Yellow,
        DarkBlue,
        Blue,
        Purple,
    }

    [Serializable]
    public struct qColor : INetworkSerializable
    {
        public qColor(byte red, byte green, byte blue) : this(red, green, blue, 255) { }
        public qColor(SysColor systemColor) : this(systemColor.R, systemColor.G, systemColor.B, systemColor.A) { }

        public qColor(byte red, byte green, byte blue, byte alpha)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
            this.alpha = alpha;
        }

        public static qColor Clear => new qColor(0, 0, 0, 0);
        public static qColor Black => new qColor(0, 0, 0);
        public static qColor White => new qColor(255, 255, 255);
        public static qColor Red => new qColor(255, 0, 0);
        public static qColor Green => new qColor(0, 255, 0);
        public static qColor Yellow => new qColor(255, 255, 0);
        public static qColor DarkBlue => new qColor(0, 0, 255);
        public static qColor Blue => new qColor(0, 255, 255);
        public static qColor Purple => new qColor(255, 0, 255);

        public byte red;
        public byte green;
        public byte blue;
        public byte alpha;

        public static qColor GetGenericColor(GenericColor color) =>
            color switch
            {
                GenericColor.Clear => Clear,
                GenericColor.Black => Black,
                GenericColor.White => White,
                GenericColor.Red => Red,
                GenericColor.Green => Green,
                GenericColor.Yellow => Yellow,
                GenericColor.DarkBlue => DarkBlue,
                GenericColor.Blue => Blue,
                GenericColor.Purple => Purple,
                _ => Clear,
            };

        public void Read(qPacket packet)
        {
            red = packet.ReadByte();
            green = packet.ReadByte();
            blue = packet.ReadByte();
            alpha = packet.ReadByte();
        }

        public qPacket Write(qPacket packet) =>
            packet
            .Write(red)
            .Write(green)
            .Write(blue)
            .Write(alpha);

        public static bool operator ==(qColor? a, qColor? b) =>
            a?.Equals(b) ?? (a is null && b is null);

        public static bool operator !=(qColor? left, qColor? right) =>
            !(left == right);

        public override bool Equals(object obj)
        {
            if (!(obj is qColor color))
                return false;

            return red == color.red &&
                green == color.green &&
                blue == color.blue &&
                alpha == color.alpha;
        }

        public override string ToString() =>
            alpha == 255 ?
                $"rgb({red}, {green}, {blue})" :
                $"rgba({red}, {green}, {blue}, {alpha})";

        public override int GetHashCode() =>
            ToString().GetHashCode();

        public SysColor ToSystem() =>
            SysColor.FromArgb(alpha, red, green, blue);

        #region Parsing
        public static bool TryParseFromName(string s, out qColor color)
        {
            var colorNames = new Dictionary<string, qColor>()
            {
                ["clear"] = Clear,
                ["black"] = Black,
                ["white"] = White,
                ["red"] = Red,
                ["green"] = Green,
                ["yellow"] = Yellow,
                ["darkBlue"] = DarkBlue,
                ["blue"] = Blue,
                ["purple"] = Purple,
            };

            return colorNames.TryGetValue(s ?? string.Empty, out color);
        }

        public static bool TryParseFromRgba(string s, out qColor color)
        {
            s ??= string.Empty;
            if (s.StartsWith("rgba(") && s.EndsWith(")"))
            {
                var parts = s.Substring(5, s.Length - 5 - 1)
                    .Split(',');

                if ((parts.Length == 3 || parts.Length == 4) &&
                    byte.TryParse(parts[0], out byte r) &&
                    byte.TryParse(parts[1], out byte g) &&
                    byte.TryParse(parts[2], out byte b))
                {
                    color = new qColor(r, g, b);
                    if (parts.Length == 4 &&
                        byte.TryParse(parts[3], out byte a))
                        color.alpha = a;

                    return true;
                }
            }

            color = new qColor();
            return false;
        }

        public static bool TryParseFromRgb(string s, out qColor color)
        {
            s ??= string.Empty;
            if (s.StartsWith("rgb(") && s.EndsWith(")"))
            {
                var parts = s.Substring(4, s.Length - 4 - 1)
                    .Split(',');

                if (parts.Length == 3 &&
                    byte.TryParse(parts[0], out byte r) &&
                    byte.TryParse(parts[1], out byte g) &&
                    byte.TryParse(parts[2], out byte b))
                {
                    color = new qColor(r, g, b);
                    return true;
                }
            }

            color = new qColor();
            return false;
        }

        public static bool TryParseRgbaFromHex(string s, out qColor color)
        {
            s ??= string.Empty;
            s = s.ToLower();
            color = Clear;

            if (s.Length != 9 || !s.StartsWith('#'))
                return false;

            var hashes = new Dictionary<char, byte>()
            {
                ['0'] = 0,
                ['1'] = 1,
                ['2'] = 2,
                ['3'] = 3,
                ['4'] = 4,
                ['5'] = 5,
                ['6'] = 6,
                ['7'] = 7,
                ['8'] = 8,
                ['9'] = 9,
                ['a'] = 10,
                ['b'] = 11,
                ['c'] = 12,
                ['d'] = 13,
                ['e'] = 14,
                ['f'] = 15,
            };

            if (!hashes.TryGetValue(s[1], out var r1) ||
                !hashes.TryGetValue(s[2], out var r2) ||
                !hashes.TryGetValue(s[3], out var g1) ||
                !hashes.TryGetValue(s[4], out var g2) ||
                !hashes.TryGetValue(s[5], out var b1) ||
                !hashes.TryGetValue(s[6], out var b2) ||
                !hashes.TryGetValue(s[7], out var a1) ||
                !hashes.TryGetValue(s[8], out var a2))
                return false;

            color.red = (byte)(r1 * 16 + r2);
            color.green = (byte)(g1 * 16 + g2);
            color.blue = (byte)(b1 * 16 + b2);
            color.alpha = (byte)(a1 * 16 + a2);
            return true;
        }

        public static bool TryParseRgbFromHex(string s, out qColor color)
        {
            s ??= string.Empty;
            s = s.ToLower();
            color = Black;

            if (s.Length != 7 || !s.StartsWith('#'))
                return false;

            var hashes = new Dictionary<char, byte>()
            {
                ['0'] = 0,
                ['1'] = 1,
                ['2'] = 2,
                ['3'] = 3,
                ['4'] = 4,
                ['5'] = 5,
                ['6'] = 6,
                ['7'] = 7,
                ['8'] = 8,
                ['9'] = 9,
                ['a'] = 10,
                ['b'] = 11,
                ['c'] = 12,
                ['d'] = 13,
                ['e'] = 14,
                ['f'] = 15,
            };

            if (!hashes.TryGetValue(s[1], out var r1) ||
                !hashes.TryGetValue(s[2], out var r2) ||
                !hashes.TryGetValue(s[3], out var g1) ||
                !hashes.TryGetValue(s[4], out var g2) ||
                !hashes.TryGetValue(s[5], out var b1) ||
                !hashes.TryGetValue(s[6], out var b2))
                return false;

            color.red = (byte)(r1 * 16 + r2);
            color.green = (byte)(g1 * 16 + g2);
            color.blue = (byte)(b1 * 16 + b2);
            return true;
        }

        public static bool TryParse(string s, out qColor color) =>
            TryParseFromName(s, out color) ||
            TryParseFromRgba(s, out color) ||
            TryParseFromRgb(s, out color) ||
            TryParseRgbaFromHex(s, out color) ||
            TryParseRgbFromHex(s, out color);

        public static qColor Parse(string s)
        {
            if (TryParse(s, out qColor color))
                return color;

            throw new FormatException($"Couldn't parse '{s}' to 'qColor'.");
        }
        #endregion
    }
}