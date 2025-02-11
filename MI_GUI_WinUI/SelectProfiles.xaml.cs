using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using MI_GUI_WinUI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MI_GUI_WinUI;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SelectProfiles : Page
{
    public SelectProfiles()
    {
        this.InitializeComponent();

        ViewModel = Ioc.Default.GetService<SelectProfilesViewModel>();

        // Set the data context for the page
        DataContext = ViewModel;
        
        // Generate the previews
        ViewModel.GenerateGuiElementsPreview();
    }

    public SelectProfilesViewModel ViewModel { get; }

}
