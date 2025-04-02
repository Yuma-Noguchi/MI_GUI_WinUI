using System.Collections.Generic;
using System.Threading.Tasks;
using MI_GUI_WinUI.Models;

namespace MI_GUI_WinUI.Services.Interfaces
{
    public interface IProfileService
    {
        Task<List<Profile>> ReadProfilesFromJsonAsync(string folderPath);
        Task DeleteProfileAsync(string profileName, string folderPath);
        Task SaveProfileAsync(Profile profile, string folderPath);
    }
}