namespace GottaManagePlus.Utils;

public static class StringExtensions
{
    /// <summary>
    /// Determines the number of consecutive matching characters from the start of two strings.
    /// </summary>
    /// <param name="s1">The first string to compare.</param>
    /// <param name="s2">The second string to compare.</param>
    /// <returns>
    /// The count of characters that match sequentially from the beginning of both strings.
    /// Returns 0 if either string is null or empty.
    /// Returns the length of the shorter string if all characters match up to that point.
    /// </returns>
    /// <example>
    /// <code>
    /// "HelloWorld".ManyStartWith("Hello") // Returns 5
    /// "Test".ManyStartWith("Testing")     // Returns 4
    /// "ABC".ManyStartWith("ABD")          // Returns 2
    /// "".ManyStartWith("Anything")        // Returns 0
    /// </code>
    /// </example>
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