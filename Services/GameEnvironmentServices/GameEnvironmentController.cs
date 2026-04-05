using System.Collections.Generic;
using GottaManagePlus.Interfaces.GameEnvironment;

namespace GottaManagePlus.Services.GameEnvironmentServices;

/// <summary>
/// A service responsible for determining the currently active environment.
/// </summary>
public sealed class GameEnvironmentController(IEnumerable<IGameEnvironmentFactory> factories)
{
    // ----- Private API -----
    private readonly IEnumerable<IGameEnvironmentFactory> _factories = factories;
    
    // ----- Public API -----
    public IGameEnvironment? CurrentEnvironment { get; private set; }

    /// <summary>
    /// Sets a new environment to the controller based on the available factories.
    /// </summary>
    /// <param name="executablePath">The path of the game's executable.</param>
    public void SetNewEnvironment(string executablePath)
    {
        // Try to get the right factory.
        foreach (var factory in _factories)
        {
            var env = factory.CreateEnvironment(executablePath);
            if (env == null) continue;
            CurrentEnvironment = env;
            return;
        }
        CurrentEnvironment = null; // No factory could handle this path.
    }

    /// <summary>
    /// Checks whether the current environment is valid or not.
    /// </summary>
    public bool IsEnvironmentValid
    {
        get
        {
            // If null, false.
            if (CurrentEnvironment == null) return false;
            
            // Try to change environment to check if a factory finds it.
            var previousEnvironment = CurrentEnvironment;
            SetNewEnvironment(CurrentEnvironment.RootPath);
            
            // If the environment remains the same, and the current environment isn't null, then true.
            if (CurrentEnvironment == previousEnvironment) return CurrentEnvironment != null;
            
            // Reset the variable's state and return false.
            CurrentEnvironment = previousEnvironment;
            return false;

        }
    }
}