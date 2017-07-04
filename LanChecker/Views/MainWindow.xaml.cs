using LanChecker.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

            Dictionary<string, string> names = null;
            if (File.Exists("mac.txt"))
            {
                var r = new Regex(@"^(.+?)\t(.+?)$");
                names = File.ReadLines("mac.txt", Encoding.Default).Select(t => r.Match(t)).Where(t => t.Success).ToDictionary(t => t.Groups[1].Value, t => t.Groups[2].Value);
            }

            InitializeComponent();
            DataContext = new MainViewModel(uint.Parse(args[1]), uint.Parse(args[2]), int.Parse(args[3]), names);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ((MainViewModel)DataContext).Dispose();
        }
    }
}
