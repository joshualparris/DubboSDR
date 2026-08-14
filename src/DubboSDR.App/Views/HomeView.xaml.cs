using System;
using System.Windows;
using System.Windows.Controls;

namespace DubboSDR.App.Views
{
    public partial class HomeView : UserControl
    {
        public event Action? OnKidsRadioRequested;
        public event Action? OnExplorerRequested;

        public HomeView()
        {
            InitializeComponent();
        }

        private void BtnKidsRadio_Click(object sender, RoutedEventArgs e)
        {
            OnKidsRadioRequested?.Invoke();
        }

        private void BtnExplorer_Click(object sender, RoutedEventArgs e)
        {
            OnExplorerRequested?.Invoke();
        }
    }
}
