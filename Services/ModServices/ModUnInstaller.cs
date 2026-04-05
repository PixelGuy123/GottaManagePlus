using Serilog;

namespace GottaManagePlus.Services.ModServices;

public class ModUnInstaller
{
    private readonly ILogger _logger;

    public ModUnInstaller(ILogger logger)
    {
        _logger = logger;
    }
}