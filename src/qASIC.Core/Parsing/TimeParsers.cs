using System;
using System.Text;

namespace qASIC.Parsing
{
    public class TimeSpanParser : ValueParser<TimeSpan>
    {
        public override bool TryParse(string s, out TimeSpan result)
        {
            result = TimeSpan.Zero;

            if (s == null)
                return false;

            var emptyBlock = false;
            var numBlock = true;
            var num = 0.0;
            var txt = new StringBuilder();

            foreach (var c in s.Trim())
            {
                //Ignore empty characters, but make sure
                //they aren't in the middle of a block
                if (char.IsWhiteSpace(c))
                {
                    //Flag that we are in an empty block
                    //If we don't change type, then
                    //string is invalid
                    emptyBlock = true;

                    continue;
                }

                if (c == ',')
                {
                    //If a comma isn't placed right behind
                    //a text segment
                    //-> invalid
                    if (emptyBlock || numBlock)
                        return false;

                    continue;
                }

                var isNum = char.IsNumber(c);

                //If block changed, apply
                if (numBlock != isNum)
                    if (!ApplyBlock(ref result))
                        return false;

                //If the block type didn't change
                //and there were empty characters
                //-> invalid
                if (emptyBlock)
                    return false;

                //We append the number to the block
                txt.Append(c);
            }

            //True if buffer is empty
            //Or if the unfinished block was text and 
            //if applying it now was successfull            
            return txt.Length == 0 || (!numBlock && ApplyBlock(ref result));


            bool ApplyBlock(ref TimeSpan span)
            {
                emptyBlock = false;

                switch (numBlock)
                {
                    case true:
                        if (!double.TryParse(txt.ToString(), out num))
                            return false;

                        break;
                    case false:
                        if (!TryGetSpan(num, txt.ToString(), out TimeSpan ts))
                            return false;

                        span += ts;
                        break;
                }

                txt.Clear();
                numBlock = !numBlock;
                return true;
            }
        }

        bool TryGetSpan(double num, string txt, out TimeSpan span)
        {
            txt = txt.ToLower();

            switch (txt)
            {
                case "miliseconds":
                case "ms":
                    span = TimeSpan.FromMilliseconds(num);
                    return true;
                case "seconds":
                case "sec":
                case "s":
                    span = TimeSpan.FromSeconds(num);
                    return true;
                case "minutes":
                case "min":
                    span = TimeSpan.FromMinutes(num);
                    return true;
                case "hours":
                case "h":
                    span = TimeSpan.FromHours(num);
                    return true;
                case "days":
                case "d":
                    span = TimeSpan.FromDays(num);
                    return true;
                case "weeks":
                    span = TimeSpan.FromDays(num * 7.0);
                    return true;
                case "months":
                case "mon":
                    span = TimeSpan.FromDays(num * 30.0);
                    return true;
                case "years":
                case "y":
                    span = TimeSpan.FromDays(num * 364.25);
                    return true;
            }

            if (num == 1.0)
            {
                switch (txt)
                {
                    case "milisecond":
                        span = TimeSpan.FromMilliseconds(1.0);
                        return true;
                    case "second":
                        span = TimeSpan.FromSeconds(1.0);
                        return true;
                    case "minute":
                        span = TimeSpan.FromMinutes(1.0);
                        return true;
                    case "hour":
                        span = TimeSpan.FromHours(1.0);
                        return true;
                    case "day":
                        span = TimeSpan.FromDays(1.0);
                        return true;
                    case "month":
                        span = TimeSpan.FromDays(30.0);
                        return true;
                    case "year":
                        span = TimeSpan.FromDays(365.0);
                        return true;
                }
            }

            span = TimeSpan.Zero;
            return false;
        }

        public override string ConvertToString(TimeSpan obj)
        {
            int years = obj.Days / 365; obj -= TimeSpan.FromDays(years * 365);
            int months = obj.Days / 12; obj -= TimeSpan.FromDays(months * 30.0);
            int days = obj.Days;
            int hours = obj.Hours;
            int minutes = obj.Minutes;
            int seconds = obj.Seconds;
            int miliseconds = obj.Milliseconds;

            var parts = new (string, int)[]
            {
                ($"{years} year", years),
                ($"{months} month", months),
                ($"{days} day", days),
                ($"{hours} hour", hours),
                ($"{minutes} minute", minutes),
                ($"{seconds} second", seconds),
                ($"{miliseconds} milisecond", miliseconds),
            };

            var start = 0;
            var end = parts.Length;

            while (start < end && parts[start].Item2 == 0)
                start++;

            while (start < end && parts[end - 1].Item2 == 0)
                end--;

            if (start == end)
                return $"0 seconds";

            var txt = new StringBuilder();
            for (int i = start; i < end; i++)
            {
                txt.Append(parts[i].Item1);
                if (parts[i].Item2 != 1)
                    txt.Append('s');

                txt.Append(' ');
            }

            return txt.ToString().TrimEnd();
        }
    }

    public class DateTimeParser : ValueParser<DateTime>
    {
        public override bool TryParse(string s, out DateTime result) =>
            DateTime.TryParse(s, out result);
    }
}