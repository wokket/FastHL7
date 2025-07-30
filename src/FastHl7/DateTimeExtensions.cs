namespace FastHl7;

/// <summary>
/// Extensions to make reading DateTime values from HL7 easier
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a string used for a `DT` (Date only) value into a <see cref="DateOnly"/> object.  If a value isn't provided in the message it is substituted for `1` in the returned object.
    /// Ie, `2024` will return a <see cref="DateOnly"/> object for 1-jan-2024.
    /// </summary>
    /// <param name="value">The field value from a message</param>
    /// <param name="throwException">Throw, rather than swallowing exceptions...</param>
    /// <remarks>This method does not allocate if successful.  For spec info see https://hl7-definition.caristix.com/v2/HL7v2.7/DataTypes/DT</remarks>
    /// <returns></returns>
    public static DateOnly? AsDate(this ReadOnlySpan<char> value, bool throwException = true)
    {
        try
        {
            if (value.IsEmpty) // this is legit
            {
                return null;
            }

            if (value.Length is < 4 or > 8) // this is not
            {
                if (throwException)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Value must be between 4 and 8 numerals long for a DT value.");
                }

                return null;
            }

            // we could have a year, year and month (6 chars), or a full date (8 chars)
            var year = int.Parse(value[..4]);
            var month = value.Length >= 6 ? int.Parse(value[4..6]) : 1;
            var day = value.Length == 8 ? int.Parse(value[6..8]) : 1;

            return new DateOnly(year, month, day);
        }
        catch (Exception)
        {
            if (throwException)
            {
                throw;
            }

            // TODO: Log?
            return null;
        }
    }

    /// <summary>
    /// Remember: "Anything involving dates and times is wrong.  If you think it's right you just haven't found the bug yet" - An old mentor.
    /// 
    /// Converts a string used for a `DTM` (DateTime) value into a <see cref="DateTimeOffset"/> object.  If a value isn't provided in the message it is subsituted for either `0` or `1` in the returned object.
    /// Ie, `2024` will return a <see cref="DateTimeOffset"/> object for midnight on 1-jan-2024.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="throwException">Throw, rather than swallowing exceptions...</param>
    /// <remarks>This method does not allocate if successful.  For spec info see https://hl7-definition.caristix.com/v2/HL7v2.7/DataTypes/DTM</remarks>
    /// <returns></returns>
    public static DateTimeOffset? AsDateTime(this ReadOnlySpan<char> value, bool throwException = true)
    {
        try
        {
            if (value.IsEmpty) // this is legit
            {
                return null;
            }

            if (value.Length is < 4 or > 24) // this is not
            {
                if (throwException)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Value must be between 4 and 24 characters for a DTM value.");
                }

                return null;
            }

            // we could have a year, year and month (6 chars), or a full date (8 chars)
            var year = int.Parse(value[..4]);
            var month = value.Length >= 6 ? int.Parse(value[4..6]) : 1;
            var day = value.Length >= 8 ? int.Parse(value[6..8]) : 1;
            var hours = value.Length >= 10 ? int.Parse(value[8..10]) : 0;
            var minutes = value.Length >= 12 ? int.Parse(value[10..12]) : 0;
            var seconds = value.Length >= 14 ? int.Parse(value[12..14]) : 0;

            // Handle fractional seconds if present
            var fractionalSeconds = 0d;
            if (value.Length > 14 && value[14] == '.')
            {
                var fractionalEnd = value.IndexOfAny('+', '-');
                if (fractionalEnd == -1)
                {
                    fractionalEnd = value.Length; // no offset, take till end
                }

                //yes, this block is from CoPilot... it took it 4 goes, but we got there...
                var fracSpan = value.Slice(15, fractionalEnd - 15);
                double frac = 0;
                var scale = 0.1;
                foreach (var t in fracSpan)
                {
                    frac += (t - '0') * scale;
                    scale *= 0.1;
                }
                fractionalSeconds = frac;
            }

            var offset = TimeSpan.Zero;  // default to UTC

            // handle timezone offset if present
            var timezoneStartIndex = value.IndexOfAny('+', '-');
            if (value.Length >= 18 && timezoneStartIndex != -1)
            {
                var offsetSign = value[timezoneStartIndex] == '+' ? 1 : -1;
                var offsetHours = int.Parse(value[(timezoneStartIndex+1)..(timezoneStartIndex+3)]);
                var offsetMinutes = int.Parse(value[(timezoneStartIndex+3)..(timezoneStartIndex+5)]);

                offset = new(offsetSign * offsetHours, offsetMinutes, 0);
            }

            var returnValue = new DateTimeOffset(year, month, day, hours, minutes, seconds, offset)
                .AddSeconds(fractionalSeconds);

            return returnValue;
        }
        catch (Exception)
        {
            if (throwException)
            {
                throw;
            }

            // TODO: Log?
            return null;
        }
    }
}