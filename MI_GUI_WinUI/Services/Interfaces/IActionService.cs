using System.Collections.Generic;
using System.Threading.Tasks;
using MI_GUI_WinUI.Models;

namespace MI_GUI_WinUI.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing action data operations
    /// </summary>
    public interface IActionService
    {
        /// <summary>
        /// Loads all actions from storage
        /// </summary>
        /// <returns>A list of all available actions</returns>
        Task<List<ActionData>> LoadActionsAsync();

        /// <summary>
        /// Saves or updates an action
        /// </summary>
        /// <param name="action">The action to save</param>
        Task SaveActionAsync(ActionData action);

        /// <summary>
        /// Deletes an action by its ID
        /// </summary>
        /// <param name="actionId">The ID of the action to delete</param>
        Task DeleteActionAsync(string actionId);

        /// <summary>
        /// Retrieves an action by its ID
        /// </summary>
        /// <param name="id">The ID of the action to retrieve</param>
        /// <returns>The requested action</returns>
        Task<ActionData> GetActionByIdAsync(string id);

        /// <summary>
        /// Retrieves an action by its name
        /// </summary>
        /// <param name="name">The name of the action to retrieve</param>
        /// <returns>The requested action</returns>
        Task<ActionData> GetActionByNameAsync(string name);

        /// <summary>
        /// Clears the action cache
        /// </summary>
        void ClearCache();
    }
}