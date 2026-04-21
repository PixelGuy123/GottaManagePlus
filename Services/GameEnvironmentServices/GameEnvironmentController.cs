using System;
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
    private IGameEnvironment? _currentEnvironment;
    
    // ----- Public API -----
    /// <summary>
    /// The current <see cref="IGameEnvironment"/>.
    /// </summary>
    public IGameEnvironment? CurrentEnvironment
    {
        get => _currentEnvironment;
        private set
        {
            var previousEnvironment = _currentEnvironment;
            _currentEnvironment = value;
            OnEnvironmentUpdate?.Invoke(value, previousEnvironment);
        }
    }
    
    /// <summary>
    /// An event that is raised every time the current environment is updated.
    /// </summary>
    public event EnvironmentUpdateHandler? OnEnvironmentUpdate;
    public delegate void EnvironmentUpdateHandler(IGameEnvironment? newEnvironment, IGameEnvironment? previousEnvironment);

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
    /// <returns><see langword="true"/> if the environment is valid; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This method is only needed to check if the current environment is still valid in storage.
    /// If <see cref="SetNewEnvironment"/> was used previously, <c>CurrentEnvironment != null</c> is a better alternative.
    /// </remarks>
    public bool IsEnvironmentValid() => CurrentEnvironment != null && IsEnvironmentValid(CurrentEnvironment.ExecutablePath);

    /// <summary>
    /// Checks whether the environment is valid or not through executable path.
    /// </summary>
    /// <returns><see langword="true"/> if the environment is valid; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This method is only needed to check if the current environment is still valid in storage.
    /// If <see cref="SetNewEnvironment"/> was used previously, <c>CurrentEnvironment != null</c> is a better alternative.
    /// </remarks>
    public bool IsEnvironmentValid(string executablePath)
    {
        // Try to find the right factory.
        IGameEnvironment? newEnvironment = null;
        foreach (var factory in _factories)
        {
            var env = factory.CreateEnvironment(executablePath);
            if (env == null) continue;
            newEnvironment = env;
        }
        return newEnvironment != null;
    }
}