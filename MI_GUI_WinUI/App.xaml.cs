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

namespace MI_GUI_WinUI
{
    public partial class App : Application
    {
        private readonly IWindowManager _windowManager;
        private readonly INavigationService _navigationService;
        private bool _isClosing;
        private readonly IServiceProvider _serviceProvider;

        public static new App Current => (App)Application.Current;
        public IServiceProvider Services => _serviceProvider;

        public App()
        {
            InitializeComponent();

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

            // Register window management and navigation
            services.AddSingleton<IWindowManager, WindowManager>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Register services
            services.AddSingleton<IActionService, ActionService>();
            services.AddSingleton<IMotionInputService, MotionInputService>();
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IStableDiffusionService, StableDiffusionService>();
            services.AddSingleton<IProfileService, ProfileService>();
            services.AddSingleton<IDialogService, DialogService>();

            // Register view models
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<SelectProfilesViewModel>();
            services.AddTransient<ActionStudioViewModel>();
            services.AddTransient<IconStudioViewModel>();
            services.AddTransient<ProfileEditorViewModel>();
            services.AddTransient<ActionConfigurationDialogViewModel>();

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

            // Build and configure services
            _serviceProvider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(_serviceProvider);

            // Initialize services
            _windowManager = _serviceProvider.GetRequiredService<IWindowManager>();
            _navigationService = _serviceProvider.GetRequiredService<INavigationService>();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Initialize main window
            if (_windowManager.MainWindow == null && !_isClosing)
            {
                _windowManager.InitializeMainWindow();
                var mainWindow = _windowManager.MainWindow;

                // Register window with navigation service
                if (mainWindow != null)
                {
                    _navigationService.RegisterWindow(mainWindow);
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
