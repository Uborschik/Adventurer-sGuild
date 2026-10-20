#if TOOLS
using System;

namespace AdventurersGuild.Core;

public static class NumericHelpers
{
    public static double Round4(double v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

    public static double RoundTo(double v, int digits)
        => Math.Round(v, digits, MidpointRounding.AwayFromZero);
}
#endif