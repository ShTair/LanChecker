using LanChecker.ViewModels;
using System;
using System.Windows;

namespace LanChecker.Views
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => DataContext as MainViewModel;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ViewModel.Stop();
        }
    }
}
