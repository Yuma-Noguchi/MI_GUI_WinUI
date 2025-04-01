using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.ViewModels;
using System;

namespace MI_GUI_WinUI.Controls
{
    public sealed partial class HeadTiltConfigurationDialog : ContentDialog
    {
        private ElementTheme _currentTheme = ElementTheme.Default;

        public HeadTiltConfigurationDialog(HeadTiltConfigurationViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            this.PrimaryButtonClick += HeadTiltConfigurationDialog_PrimaryButtonClick;
        }

        public HeadTiltConfigurationViewModel ViewModel { get; }

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

        public void Configure(PoseGuiElement? element, Action<PoseGuiElement> onSave)
        {
            ViewModel.Configure(element, onSave);
        }

        private void HeadTiltConfigurationDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ViewModel.SaveCommand.Execute(null);
            args.Cancel = !string.IsNullOrEmpty(ViewModel.ValidationMessage);
        }
    }
}