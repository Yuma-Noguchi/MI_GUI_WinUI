using System.Collections.Generic;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing Motion Input operations
    /// </summary>
    public interface IMotionInputService
    {
        /// <summary>
        /// Starts a specific profile
        /// </summary>
        /// <param name="profileName">Name of the profile to start</param>
        /// <returns>True if profile started successfully</returns>
        Task<bool> StartAsync(string profileName);

        /// <summary>
        /// Stops a specific profile
        /// </summary>
        /// <param name="profileName">Name of the profile to stop</param>
        /// <returns>True if profile stopped successfully</returns>
        Task<bool> StopAsync(string profileName);

        /// <summary>
        /// Launches the Motion Input service
        /// </summary>
        /// <returns>True if service launched successfully</returns>
        Task<bool> LaunchAsync();

        /// <summary>
        /// Gets a list of all available profiles
        /// </summary>
        /// <returns>List of profile names</returns>
        Task<List<string>> GetAvailableProfilesAsync();
    }
}