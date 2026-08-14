using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DubboSDR.Core;
using DubboSDR.App.Services;
using DubboSDR.App.Views;

namespace DubboSDR.App
{
    public partial class MainWindow : Window
    {
        private RadioService _radioService;
        private List<Station> _stations = new();
        
        private HomeView _homeView;
        private KidsRadioView _kidsView;
        private ExplorerView _explorerView;

        public MainWindow()
        {
            InitializeComponent();
            
            _radioService = new RadioService();
            _homeView = new HomeView();
            _kidsView = new KidsRadioView();
            _explorerView = new ExplorerView();

            _homeView.OnKidsRadioRequested += () => MainContent.Content = _kidsView;
            _homeView.OnExplorerRequested += () => MainContent.Content = _explorerView;

            _kidsView.OnBackRequested += () => MainContent.Content = _homeView;
            _explorerView.OnBackRequested += () => MainContent.Content = _homeView;

            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;

            MainContent.Content = _homeView;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ensure data directory exists relative to execution path
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                
                // In dev, data is in C:\dev\DubboSDR\data
                // In publish, it might need to be copied.
                // Let's check both paths.
                string dataPath = Path.Combine(basePath, "data", "stations.json");
                if (!File.Exists(dataPath))
                {
                    // Fallback to absolute dev path if missing in output
                    dataPath = @"C:\dev\DubboSDR\data\stations.json"; 
                }

                var repo = new StationRepository(dataPath);
                _stations = await repo.LoadStationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load stations.json: {ex.Message}");
            }

            _kidsView.Initialize(_radioService, _stations);
            _explorerView.Initialize(_radioService, _stations);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                if (WindowStyle == WindowStyle.None)
                {
                    WindowStyle = WindowStyle.SingleBorderWindow;
                    WindowState = WindowState.Normal;
                }
                else
                {
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Maximized;
                }
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _radioService?.Dispose();
        }
    }
}