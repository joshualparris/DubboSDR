using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DubboSDR.Core;
using DubboSDR.App.Services;

namespace DubboSDR.App.Views
{
    public class KidsStationViewModel
    {
        public Station Station { get; set; }
        public Brush BackgroundColor { get; set; }
        public string DisplayName => Station.KidsLabel ?? Station.Name;
        public string Subtitle => "Internet";
        public Visibility SubtitleVisibility => Station.Category == "InternetAudio" ? Visibility.Visible : Visibility.Collapsed;

        public KidsStationViewModel(Station station, string hexColor)
        {
            Station = station;
            BackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
        }
    }

    public partial class KidsRadioView : UserControl
    {
        private RadioService? _radioService;
        private List<Station> _stations = new();
        private bool _isPlaying = false;
        
        public event Action? OnBackRequested;

        public KidsRadioView()
        {
            InitializeComponent();
        }

        public void Initialize(RadioService radioService, List<Station> allStations)
        {
            _radioService = radioService;
            _stations = allStations.Where(s => s.ShowInKidsMode).ToList();
            
            _radioService.DeviceManager.OnError -= DeviceManager_OnError;
            _radioService.DeviceManager.OnError += DeviceManager_OnError;
            
            BuildStationCards();
        }

        private void DeviceManager_OnError(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                TxtStatusMsg.Text = "📡 The radio isn't connected.\nAsk a grown-up for help.";
                TxtStatusMsg.Visibility = Visibility.Visible;
            });
        }

        private void BuildStationCards()
        {
            string[] colors = { "#FFCDD2", "#F8BBD0", "#E1BEE7", "#D1C4E9", "#C5CAE9", "#BBDEFB", "#B3E5FC", "#B2EBF2", "#B2DFDB", "#C8E6C9", "#DCEDC8", "#F0F4C3", "#FFF9C4", "#FFECB3", "#FFE0B2", "#FFCCBC" };
            
            var viewModels = new List<KidsStationViewModel>();
            for (int i = 0; i < _stations.Count; i++)
            {
                viewModels.Add(new KidsStationViewModel(_stations[i], colors[i % colors.Length]));
            }

            StationsItemsControl.ItemsSource = viewModels;
        }

        private async void StationBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Station station && _radioService != null)
            {
                TxtStatusMsg.Visibility = Visibility.Collapsed;
                TxtNowIcon.Text = station.KidsIcon ?? "🎵";
                TxtNowName.Text = "Tuning...";
                TxtNowFreq.Text = station.Category == "InternetAudio" ? "Internet Stream" : $"{station.FrequencyHz / 1000000.0:F1} FM";
                
                TxtNowName.Foreground = Brushes.Orange; // "Tuning" state feedback
                
                if (station.Category == "InternetAudio")
                {
                    TxtStatusMsg.Text = "🎵 (Internet streaming coming soon!)";
                    TxtStatusMsg.Foreground = Brushes.Gray;
                    TxtStatusMsg.Visibility = Visibility.Visible;
                }
                
                // Disable button momentarily
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
                        TxtNowName.Text = station.KidsLabel ?? station.Name;
                        TxtNowName.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                        BtnPlayPause.Content = "⏸ PAUSE";
                        BtnPlayPause.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                        _isPlaying = true;
                    }
                    else
                    {
                        // If it failed or was cancelled, leave it alone (a newer tune is handling UI)
                    }
                }
                catch (Exception)
                {
                    TxtStatusMsg.Text = "📻 I can't hear this station clearly.";
                    TxtStatusMsg.Foreground = Brushes.Red;
                    TxtStatusMsg.Visibility = Visibility.Visible;
                    TxtNowName.Text = station.KidsLabel ?? station.Name;
                    TxtNowName.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                }
                finally
                {
                    btn.IsEnabled = true;
                }
            }
        }

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_radioService == null) return;

            if (_isPlaying)
            {
                _radioService.Pause();
                BtnPlayPause.Content = "▶ PLAY";
                BtnPlayPause.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                _isPlaying = false;
            }
            else
            {
                _radioService.Resume();
                BtnPlayPause.Content = "⏸ PAUSE";
                BtnPlayPause.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                _isPlaying = true;
            }
        }

        private void SldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _radioService?.SetVolume((float)e.NewValue);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            _radioService?.Pause(); // Stop audio when leaving Kids mode
            OnBackRequested?.Invoke();
        }
    }
}
