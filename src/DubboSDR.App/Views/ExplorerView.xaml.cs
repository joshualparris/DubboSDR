using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DubboSDR.Core;
using DubboSDR.App.Services;

namespace DubboSDR.App.Views
{
    public class ExplorerStationViewModel
    {
        public Station Station { get; set; }
        public string DisplayName => $"{Station.FrequencyHz / 1000000.0:F1} {Station.Name}";
        
        public ExplorerStationViewModel(Station station)
        {
            Station = station;
        }
    }

    public partial class ExplorerView : UserControl
    {
        private RadioService? _radioService;
        private List<Station> _stations = new();
        private bool _isPlaying = false;
        
        public event Action? OnBackRequested;

        public ExplorerView()
        {
            InitializeComponent();
        }

        public void Initialize(RadioService radioService, List<Station> allStations)
        {
            _radioService = radioService;
            _stations = allStations;

            _radioService.DeviceManager.OnSignalStrengthUpdated -= DeviceManager_OnSignalStrengthUpdated;
            _radioService.DeviceManager.OnDebugInfoUpdated -= DeviceManager_OnDebugInfoUpdated;
            _radioService.DeviceManager.OnError -= DeviceManager_OnError;
            
            _radioService.DeviceManager.OnSignalStrengthUpdated += DeviceManager_OnSignalStrengthUpdated;
            _radioService.DeviceManager.OnDebugInfoUpdated += DeviceManager_OnDebugInfoUpdated;
            _radioService.DeviceManager.OnError += DeviceManager_OnError;

            BuildStationCards();
        }

        private void BuildStationCards()
        {
            var viewModels = new List<ExplorerStationViewModel>();
            foreach (var station in _stations)
            {
                viewModels.Add(new ExplorerStationViewModel(station));
            }
            ExplorerStationsItemsControl.ItemsSource = viewModels;
        }

        private async void Station_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Station station && _radioService != null)
            {
                TxtPlayingName.Text = "Tuning...";
                TxtPlayingName.Foreground = Brushes.Orange;
                TxtPlayingFreq.Text = station.Category == "InternetAudio" ? "Internet Stream" : $"{station.FrequencyHz / 1000000.0:F3} MHz · {station.Mode}";
                TxtDbgFreq.Text = station.Category == "InternetAudio" ? "Hardware Idle" : $"Frequency: {station.FrequencyHz / 1000000.0:F3} MHz (HW: {(station.FrequencyHz - 240000) / 1000000.0:F3} MHz)";
                TxtStatus.Text = "● Tuning...";
                TxtStatus.Foreground = Brushes.Orange;
                
                btn.IsEnabled = false;

                try
                {
                    if (station.Category != "InternetAudio")
                    {
                        if (!_radioService.Connect()) 
                        {
                            btn.IsEnabled = true;
                            return;
                        }
                    }
                    
                    bool success = await _radioService.TuneAsync(station);
                    
                    if (success)
                    {
                        TxtPlayingName.Text = station.Name;
                        TxtPlayingName.Foreground = Brushes.White;
                        BtnPlayPause.Content = "■ Stop";
                        BtnPlayPause.Background = new SolidColorBrush(Color.FromRgb(136, 0, 0)); // #880000
                        _isPlaying = true;
                        TxtStatus.Text = "● Connected";
                        TxtStatus.Foreground = Brushes.LightGreen;
                    }
                }
                finally
                {
                    btn.IsEnabled = true;
                }
            }
        }

        private void DeviceManager_OnError(string message)
        {
            Dispatcher.InvokeAsync(() =>
            {
                TxtStatus.Text = message;
                TxtStatus.Foreground = Brushes.Red;
            });
        }

        private void DeviceManager_OnSignalStrengthUpdated(double rms)
        {
            Dispatcher.InvokeAsync(() =>
            {
                ProgSignal.Value = rms;
            });
        }
        
        private void DeviceManager_OnDebugInfoUpdated(string dbgInfo)
        {
            Dispatcher.InvokeAsync(() =>
            {
                TxtDbgRates.Text = dbgInfo;
            });
        }

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_radioService == null) return;

            if (_isPlaying)
            {
                _radioService.Pause();
                BtnPlayPause.Content = "▶ Play";
                BtnPlayPause.Background = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                _isPlaying = false;
                ProgSignal.Value = 0;
            }
            else
            {
                _radioService.Resume();
                BtnPlayPause.Content = "■ Stop";
                BtnPlayPause.Background = new SolidColorBrush(Color.FromRgb(136, 0, 0));
                _isPlaying = true;
            }
        }

        private void SldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _radioService?.SetVolume((float)e.NewValue);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            _radioService?.Pause(); // Stop audio when leaving mode
            OnBackRequested?.Invoke();
        }
    }
}
