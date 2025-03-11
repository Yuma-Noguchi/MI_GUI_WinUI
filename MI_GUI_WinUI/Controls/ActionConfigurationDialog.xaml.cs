using Microsoft.UI.Xaml.Controls;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.ViewModels;
using System;

namespace MI_GUI_WinUI.Controls
{
    public sealed partial class ActionConfigurationDialog : ContentDialog
    {
        public ActionConfigurationDialog()
        {
            this.InitializeComponent();
            ViewModel = new ActionConfigurationDialogViewModel();
            this.Closing += ActionConfigurationDialog_Closing;
            this.PrimaryButtonClick += ActionConfigurationDialog_PrimaryButtonClick;
        }

        public ActionConfigurationDialogViewModel ViewModel { get; }

        public void Configure(UnifiedGuiElement element, Action<UnifiedGuiElement> onSave)
        {
            ViewModel.Initialize(element, onSave);
        }

        private void ActionConfigurationDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (args.Result == ContentDialogResult.Primary)
            {
                if (!string.IsNullOrEmpty(ViewModel.ValidationMessage))
                {
                    args.Cancel = true;
                }
            }
        }

        private void ActionConfigurationDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = !string.IsNullOrEmpty(ViewModel.ValidationMessage);
            ViewModel.SaveCommand.Execute(null);
        }
    }
}
