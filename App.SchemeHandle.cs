using Avalonia.Controls.ApplicationLifetimes;

namespace GottaManagePlus;

public partial class App
{
    private void SetupHandle(IActivatableLifetime activatableLifetime)
    {
        activatableLifetime.Activated += (_, e) =>
        {
            switch (e)
            {
                case ProtocolActivatedEventArgs protocolArgs:
                    Console.WriteLine($"Protocol: {protocolArgs.Uri}");
                    break;
                case FileActivatedEventArgs fileArgs:
                    Console.WriteLine($"File: {fileArgs.Files.FirstOrDefault()?.Path}");
                    break;
            }
        };
    }
}