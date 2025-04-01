using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.ViewModels;
using System;

namespace MI_GUI_WinUI.Controls
{
    public sealed partial class ActionConfigurationDialog : ContentDialog
    {
        private ElementTheme _currentTheme = ElementTheme.Default;

        public ActionConfigurationDialog(ActionConfigurationDialogViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            this.PrimaryButtonClick += ActionConfigurationDialog_PrimaryButtonClick;
        }

        public ActionConfigurationDialogViewModel ViewModel { get; }

        public new XamlRoot? XamlRoot
        {
            get => base.XamlRoot;
            set
            {
                base.XamlRoot = value;
                if (value?.Content is FrameworkElement element)
                {
                    _currentTheme = element.ActualTheme;
                    this.RequestedTheme = _currentTheme;
                }
            }
        }

        public async void Configure(UnifiedGuiElement element, Action<UnifiedGuiElement> onSave)
        {
            await ViewModel.Configure(element, onSave);
        }

        private void ActionConfigurationDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ViewModel.SaveCommand.Execute(null);
            args.Cancel = !string.IsNullOrEmpty(ViewModel.ValidationMessage);
        }
    }
}
