using System;

namespace GottaManagePlus.Utils;

public static class StringExtensions
{
    public static int ManyStartWith(this string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;
        
        for (var i = 0; i < s1.Length && i < s2.Length; i++)
        {
            if (s1[i] != s2[i])
                return i;
        }
        return Math.Min(s1.Length, s2.Length);
    }
}