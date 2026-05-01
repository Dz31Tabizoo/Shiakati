using Microsoft.Extensions.DependencyInjection;
using Shiakati.ViewModels;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Shiakati.Views
{
    /// <summary>
    /// Logique d'interaction pour MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            this.DataContext = App.ServiceProvider?.GetRequiredService<MainViewModel>();
            this.WindowState = WindowState.Maximized;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32")]
        public static extern bool ReleaseCapture();
        private void pnlControlBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            ReleaseCapture(); // Important: Release the mouse capture first
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SendMessage(helper.Handle, 0xA1, 0x2, 0);

        }

        private void pnlControlBar_MouseEnter(object sender, MouseEventArgs e)
        {
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
        }


        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
        }
        private void BtnMinmize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Helpers.WindowHelper.CloseApp();
        }


    }
}
