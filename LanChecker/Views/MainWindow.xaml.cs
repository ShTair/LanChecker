using LanChecker.ViewModels;
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
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            ((MainViewModel)DataContext).Dispose();
        }
    }
}
