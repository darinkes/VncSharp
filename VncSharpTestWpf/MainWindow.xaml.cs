using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VncSharp;

namespace VncSharpTestWpf
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var stream = new FileStream("example2.fbs", FileMode.Open);
            remoteDesktop.ConnectComplete += RemoteDesktop1OnConnectComplete;
            remoteDesktop.ConnectionLost += RemoteDesktop1OnConnectionLost;
            try
            {
                remoteDesktop.Connect(stream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void RemoteDesktop1OnConnectionLost(object sender, EventArgs eventArgs)
        {
        }

        private void RemoteDesktop1OnConnectComplete(object sender, ConnectEventArgs connectEventArgs)
        {
            Width = connectEventArgs.DesktopWidth + 20;
            Height = connectEventArgs.DesktopHeight + 50;
        }
    }
}
