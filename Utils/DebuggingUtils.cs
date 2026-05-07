#if DEBUG
using Avalonia;
using Avalonia.Controls;
using Serilog;

namespace GottaManagePlus.Utils;

public static class DebuggingUtils
{
    public static void PrintAllResources()
    {
        // Start the search from the Application's resources and styles
        var app = Application.Current;
        if (app is null) return;

        // A HashSet to avoid printing the same resource multiple times
        var printedKeys = new HashSet<object>();

        // Start the inspection from the Application's root resources and styles
        Log.Logger.Information("Logging application resources!");
        InspectProvider(app.Resources);
        InspectProvider(app.Styles);
        return;

        // Helper function to recurse through a resource provider
        void InspectProvider(IResourceProvider provider, int depth = 0)
        {
            switch (provider)
            {
                case ResourceDictionary dict:
                {
                    // Inspect the dictionary's own resources
                    foreach (var entry in dict)
                    {
                        if (printedKeys.Add(entry.Key))
                        {
                            Log.Logger.Information("{S}Key: {EntryKey}, Value: {EntryValue}", new string(' ', depth), entry.Key, entry.Value);
                        }
                    }

                    // Crucial: Recurse through ANY merged dictionaries
                    foreach (var mergedDict in dict.MergedDictionaries)
                        InspectProvider(mergedDict, depth + 2);

                    // Important: Theme dictionaries are stored in a special collection
                    // and are not automatically included in the enumeration above.
                    foreach (var themeDict in dict.ThemeDictionaries.Values)
                        InspectProvider(themeDict, depth + 2);

                    break;
                }
                case Avalonia.Styling.Styles styles:
                {
                    // Styles can also have their own resources
                    foreach (var style in styles)
                        switch (style)
                        {
                            case Avalonia.Styling.Styles nestedStyles:
                                InspectProvider(nestedStyles, depth);
                                break;
                            case IResourceProvider styleResourceProvider:
                                InspectProvider(styleResourceProvider, depth);
                                break;
                        }

                    break;
                }
            }
        }
    }
}
#endif