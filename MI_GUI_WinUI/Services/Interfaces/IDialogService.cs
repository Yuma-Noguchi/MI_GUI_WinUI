using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;

namespace MI_GUI_WinUI.Services.Interfaces
{
    public interface IDialogService
    {
        Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog);
        Task ShowErrorAsync(Window window, string title, string message);
    }
}