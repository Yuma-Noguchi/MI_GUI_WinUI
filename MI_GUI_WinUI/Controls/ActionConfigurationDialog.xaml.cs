using Microsoft.UI.Xaml.Controls;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.ViewModels;
using System;

namespace MI_GUI_WinUI.Controls
{
    public sealed partial class ActionConfigurationDialog : ContentDialog
    {
        public ActionConfigurationDialog(ActionConfigurationDialogViewModel viewModel)
        {
            this.InitializeComponent();
            ViewModel = viewModel;
            this.PrimaryButtonClick += ActionConfigurationDialog_PrimaryButtonClick;
        }

        public ActionConfigurationDialogViewModel ViewModel { get; }

        public void Configure(UnifiedGuiElement element, Action<UnifiedGuiElement> onSave)
        {
            ViewModel.Initialize(element, onSave);
        }

        private void ActionConfigurationDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ViewModel.SaveCommand.Execute(null);
            args.Cancel = !string.IsNullOrEmpty(ViewModel.ValidationMessage);
        }
    }
}
