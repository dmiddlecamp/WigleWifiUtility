using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace wifiLogReader
{
    public static class Utilities
    {


        /// <summary>
        /// Never worry about DBNull, or .Net type conversion again!
        /// (usually fast, and safe!)
        /// </summary>
        public static T GetAs<T>(object value, T ifEmpty)
        {
            try
            {
                if ((!Convert.IsDBNull(value)) && (value != null))
                {
                    Type destType = typeof(T);
                    Type srcType = value.GetType();

                    if (srcType == destType)
                    {
                        return (T)value;
                    }
                    else if ((srcType == typeof(string)) && (string.IsNullOrEmpty(value as string)))
                    {
                        return ifEmpty;
                    }
                    else if ((srcType == typeof(double)) && (destType == typeof(string)))
                    {
                        //this is a special case for preserving as much precision on doubles as possible.
                        return (T)Convert.ChangeType(((double)value).ToString("R"), destType);
                    }
                    else if ((srcType == typeof(string)) && (destType == typeof(bool)))
                    {
                        //special case for lowercase true/false
                        //this recognizes T, t, True, and true as all true, anything else is false.
                        string t = value as string;
                        value = ((!string.IsNullOrEmpty(t)) && ((t.ToLower() == true.ToString().ToLower()) || (t.ToLower() == "t")));
                        return (T)value;
                    }
                    else if ((srcType == typeof(string)) && (destType.IsEnum))
                    {
                        return (T)Enum.Parse(destType, (string)value);
                    }
                    else if ((destType == typeof(DateTime)) && (srcType == typeof(string)))
                    {
                        //get the ticks since Jan 1, 1601 (C# epoch) to the JS Epoch
                        DateTime JSepoch = new DateTime(1969, 12, 31, 0, 0, 0, DateTimeKind.Unspecified);
                        //value = DateTime.FromFileTime((ticks * TimeSpan.TicksPerMillisecond) + JSepoch.ToFileTime());

                        //meant to convert a javascript (new Date().getTime()) result to a C# DateTime...
                        long ticks = -1;
                        if (long.TryParse((string)value, out ticks))
                            value = JSepoch.Add(TimeSpan.FromMilliseconds(ticks));
                        else
                            value = DateTime.Parse((string)value);

                        return (T)value;
                    }
                    //add (datetime -> string) / (string -> datetime) conversions?
                    else
                    {
                        return (T)Convert.ChangeType(value, destType);
                    }
                }
            }
            catch { }
            return ifEmpty;
        }


    }
}
