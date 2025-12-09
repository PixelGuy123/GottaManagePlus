using System;

namespace GottaManagePlus.Modules.Utils;

public static class PrecisionMath
{
    public const double Epsilon = 0.001;
    public static bool PreciseEquals(this double num, double num2) =>
        Math.Abs(num - num2) < Epsilon;
}