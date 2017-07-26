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
            Dictionary<string, DeviceInfo> names = null;
            if (File.Exists("mac.txt"))
            {
                names = DeviceInfo.Load("mac.txt").ToDictionary(t => t.MacAddress);
            }

            InitializeComponent();
            DataContext = new MainViewModel(names);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ((MainViewModel)DataContext).Stop();
        }
    }
}
