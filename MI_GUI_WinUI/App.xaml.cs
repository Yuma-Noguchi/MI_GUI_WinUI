﻿using Microsoft.UI.Xaml;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.ViewModels;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Services.Interfaces;
using MI_GUI_WinUI.Pages;
using MI_GUI_WinUI.Converters;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.UI.Xaml.Controls;

namespace MI_GUI_WinUI
{
    public partial class App : Application
    {
        private readonly IWindowManager _windowManager;
        private readonly INavigationService _navigationService;
        private readonly IPageFactory _pageFactory;
        private bool _isClosing;
        private readonly IServiceProvider _serviceProvider;

        public static new App Current => (App)Application.Current;
        public IServiceProvider Services => _serviceProvider;

        public App()
        {
            // Configure services
            var services = new ServiceCollection();

            // Register logging
            services.AddSingleton<LoggingService>();
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.Services.AddSingleton<ILoggerProvider>(sp =>
                    new CustomLoggerProvider(sp.GetRequiredService<LoggingService>()));
            });

            // Register core services
            services.AddSingleton<IPageFactory, PageFactory>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Register view models first since WindowManager depends on MainWindowViewModel
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<SelectProfilesViewModel>();
            services.AddTransient<ActionStudioViewModel>();
            services.AddTransient<IconStudioViewModel>();
            services.AddTransient<ProfileEditorViewModel>();
            services.AddTransient<ActionConfigurationDialogViewModel>();
            services.AddTransient<HeadTiltConfigurationViewModel>();

            // Register window management after its dependencies
            services.AddSingleton<IWindowManager, WindowManager>();

            // Register other services
            services.AddSingleton<IActionService, ActionService>();
            services.AddSingleton<IMotionInputService, MotionInputService>();
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IStableDiffusionService, StableDiffusionService>();
            services.AddSingleton<IProfileService, ProfileService>();
            services.AddSingleton<IDialogService, DialogService>();

            // Register converters
            services.AddSingleton<StringToBoolConverter>();
            services.AddSingleton<BoolToVisibilityInverseConverter>();
            services.AddSingleton<BoolToVisibilityConverter>();
            services.AddSingleton<NumberToVisibilityConverter>();
            services.AddSingleton<NullToBoolConverter>();
            services.AddSingleton<NumberToStringConverter>();
            services.AddSingleton<BoolToIntConverter>();

            // Register pages
            services.AddTransient<HomePage>();
            services.AddTransient<SelectProfilesPage>();
            services.AddTransient<ActionStudioPage>();
            services.AddTransient<IconStudioPage>();
            services.AddTransient<ProfileEditorPage>();

            // Register controls
            services.AddTransient<Controls.PageHeader>();
            services.AddTransient<Controls.ActionConfigurationDialog>();
            services.AddTransient<Controls.HeadTiltConfigurationDialog>();

            // Build and configure services
            _serviceProvider = services.BuildServiceProvider();

            // Initialize services
            _pageFactory = _serviceProvider.GetRequiredService<IPageFactory>();
            _windowManager = _serviceProvider.GetRequiredService<IWindowManager>();
            _navigationService = _serviceProvider.GetRequiredService<INavigationService>();

            // Configure IoC container
            Ioc.Default.ConfigureServices(_serviceProvider);
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            if (_isClosing) return;

            // Initialize main window
            _windowManager.InitializeMainWindow();
            var mainWindow = _windowManager.MainWindow;
            
            if (mainWindow != null)
            {
                // Update MainWindow with NavigationService
                if (mainWindow is MainWindow main)
                {
                    main.SetNavigationService(_navigationService);
                }

                // Register window with navigation service
                _navigationService.RegisterWindow(mainWindow);

                // Find and register the content frame
                if (mainWindow.Content is FrameworkElement element)
                {
                    var frame = element.FindName("ContentFrame") as Frame;
                    if (frame != null)
                    {
                        _navigationService.Initialize(frame);
                        _navigationService.RegisterFrame(mainWindow, frame);
                    }
                }
            }
        }

        public new void Exit()
        {
            _isClosing = true;
            base.Exit();
        }
    }
}
