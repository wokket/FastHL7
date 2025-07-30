namespace FastHl7;

/// <summary>
/// Extensions to make reading DateTime values from HL7 easier
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a string used for a `DT` (Date only) value into a <see cref="DateOnly"/> object.  If a value isn't provided in the message it is subsituted for `1` in the returned object.
    /// Ie, `2024` will return a <see cref="DateOnly"/> object for 1-jan-2024.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="throwException">Throw, rather than swallowing exceptions...</param>
    /// <remarks>This method does not allocate.  For spec info see https://hl7-definition.caristix.com/v2/HL7v2.7/DataTypes/DT</remarks>
    /// <returns></returns>
    public static DateOnly? AsDtValue(this ReadOnlySpan<char> value, bool throwException = true)
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
}