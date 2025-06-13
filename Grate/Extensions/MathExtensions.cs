using System;
using System.Collections.Generic;

namespace Grate.Extensions;

public static class MathExtensions
{
    private static readonly Random rng = new();

    public static int Wrap(int x, int min, int max)
    {
        var range = max - min;
        var result = (x - min) % range;
        if (result < 0) result += range;
        return result + min;
    }


    public static float Map(float x, float a1, float a2, float b1, float b2)
    {
        // Calculate the range differences
        var inputRange = a2 - a1;
        var outputRange = b2 - b1;

        // Calculate the normalized value of x within the input range
        var normalizedValue = (x - a1) / inputRange;

        // Map the normalized value to the output range
        var mappedValue = b1 + normalizedValue * outputRange;

        return mappedValue;
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = rng.Next(n + 1);
            var value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}