// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.ViewModels;
using System;
using System.Collections.Generic;

namespace MI_GUI_WinUI.Controls
{
    /// <summary>
    /// A dialog for configuring head tilt settings.
    /// </summary>
    public sealed partial class HeadTiltConfigurationDialog : ContentDialog
    {
        private ElementTheme _currentTheme = ElementTheme.Default;

        /// <summary>
        /// Gets the view model associated with this dialog.
        /// </summary>
        public HeadTiltConfigurationViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the HeadTiltConfigurationDialog.
        /// </summary>
        /// <param name="viewModel">The view model for head tilt configuration.</param>
        public HeadTiltConfigurationDialog(HeadTiltConfigurationViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            this.InitializeComponent();
            this.PrimaryButtonClick += HeadTiltConfigurationDialog_PrimaryButtonClick;
            this.CloseButtonClick += HeadTiltConfigurationDialog_CloseButtonClick;
        }

        /// <summary>
        /// Gets or sets the XamlRoot for the dialog, handling theme changes.
        /// </summary>
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

        /// <summary>
        /// Configures the dialog with the specified settings.
        /// </summary>
        /// <param name="element">The pose element to configure.</param>
        /// <param name="buttons">Available buttons for skin selection.</param>
        /// <param name="onSave">Callback to execute when saving changes.</param>
        public void Configure(PoseGuiElement? element, IEnumerable<EditorButton> buttons, Action<PoseGuiElement> onSave)
        {
            ViewModel.Configure(element, buttons, onSave);
        }

        private void HeadTiltConfigurationDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                ViewModel.SaveCommand.Execute(null);
                args.Cancel = ViewModel.HasValidationMessage;
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void HeadTiltConfigurationDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                ViewModel.CancelCommand.Execute(null);
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
}