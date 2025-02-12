using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MI_GUI_WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
    }

    private void IconStudioButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ActionStudioButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private void SelectProfilesButton_Click(object sender, RoutedEventArgs e)
    {
        // Create and initialize the SelectProfiles page
        var selectProfilesPage = new SelectProfiles();
        selectProfilesPage.Initialize(this);
        
        // Set it as the window content
        this.Content = selectProfilesPage;
    }

    private void ProfileEditorButton_Click(object sender, RoutedEventArgs e)
    {

    }
}
