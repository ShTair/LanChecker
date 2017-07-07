using LanChecker.Models;
using LanChecker.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            Dictionary<string, DeviceInfo> names = null;
            if (File.Exists("mac.txt"))
            {
                names = DeviceInfo.Load("mac.txt").ToDictionary(t => t.MacAddress);
            }

            InitializeComponent();
            DataContext = new MainViewModel(uint.Parse(args[1]), uint.Parse(args[2]), int.Parse(args[3]), names);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ((MainViewModel)DataContext).Stop();
        }
    }
}
