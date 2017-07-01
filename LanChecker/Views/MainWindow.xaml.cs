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
        public MainWindow()
        {
            var args = Environment.GetCommandLineArgs();

            InitializeComponent();
            DataContext = new MainViewModel(uint.Parse(args[1]), uint.Parse(args[2]), int.Parse(args[3]));
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ((MainViewModel)DataContext).Dispose();
        }
    }
}
