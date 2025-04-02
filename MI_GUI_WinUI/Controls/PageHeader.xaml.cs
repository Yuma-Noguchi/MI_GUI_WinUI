using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.Pages;
using System;

namespace MI_GUI_WinUI.Controls
{
    public sealed partial class PageHeader : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(PageHeader), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty HomeButtonEnabledProperty =
            DependencyProperty.Register(nameof(HomeButtonEnabled), typeof(bool), typeof(PageHeader), new PropertyMetadata(true));

        public static readonly DependencyProperty NavigationServiceProperty =
            DependencyProperty.Register(nameof(NavigationService), typeof(INavigationService), typeof(PageHeader), 
                new PropertyMetadata(null, OnNavigationServiceChanged));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public bool HomeButtonEnabled
        {
            get => (bool)GetValue(HomeButtonEnabledProperty);
            set => SetValue(HomeButtonEnabledProperty, value);
        }

        public INavigationService NavigationService
        {
            get => (INavigationService)GetValue(NavigationServiceProperty);
            set => SetValue(NavigationServiceProperty, value);
        }

        public PageHeader()
        {
            InitializeComponent();

            // Initialize with safe defaults
            HomeButtonEnabled = true;
            Title = string.Empty;

            // Get the navigation service from IoC
            NavigationService = Ioc.Default.GetService<INavigationService>();
        }

        private static void OnNavigationServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PageHeader header)
            {
                header.HomeButtonEnabled = e.NewValue != null;
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NavigationService != null)
                {
                    NavigationService.Navigate<HomePage>();
                }
                else
                {
                    // Try to get the service again
                    NavigationService = Ioc.Default.GetService<INavigationService>();
                    NavigationService?.Navigate<HomePage>();
                }
            }
            catch (Exception)
            {
                HomeButtonEnabled = false;
            }
        }
    }
}
