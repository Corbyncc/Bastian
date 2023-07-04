using System;

namespace Bastian.Utils;
public static class TimeConverter
{
    public static long GetEpochTimestamp(string input)
    {
        input = input.Replace(" ", ""); // Remove spaces from the input string

        TimeSpan duration = ParseDuration(input);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset result = now.Add(duration);
        return result.ToUnixTimeSeconds();
    }

    public static TimeSpan ParseDuration(string input)
    {
        int seconds = 0;
        int minutes = 0;
        int hours = 0;
        int days = 0;
        int weeks = 0;

        int index = 0;
        int value = 0;
        while (index < input.Length)
        {
            char character = input[index];
            if (char.IsDigit(character))
            {
                int endIndex = index + 1;
                while (endIndex < input.Length && char.IsDigit(input[endIndex]))
                    endIndex++;

                if (!int.TryParse(input.AsSpan(index, endIndex - index), out value))
                    return TimeSpan.Zero;
                index = endIndex;
            }
            else
            {
                switch (character)
                {
                    case 's':
                        seconds = value;
                        break;
                    case 'm':
                        minutes = value;
                        break;
                    case 'h':
                        hours = value;
                        break;
                    case 'd':
                        days = value;
                        break;
                    case 'w':
                        weeks = value;
                        break;
                }

                index++;
                value = 0;
            }
        }

        TimeSpan duration = new(weeks * 7 + days, hours, minutes, seconds);
        return duration;
    }
}
