using System;
using System.IO;
using System.Windows;
using VncSharp;

namespace VncSharpTestWpf
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string vncHost = "127.0.0.1";

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                remoteDesktop.ConnectComplete += RemoteDesktop1OnConnectComplete;
                remoteDesktop.ConnectionLost += RemoteDesktop1OnConnectionLost;
                //var stream = new FileStream("b.fbs", FileMode.Open);
                //remoteDesktop.Connect(stream);
                remoteDesktop.VncPort = 63913;
                remoteDesktop.Connect(vncHost, 0, false, true, "Tor!Tor!");
            }
            catch (VncProtocolException vex)
            {
                MessageBox.Show(this,
                                string.Format("Unable to connect to VNC host:\n\n{0}.\n\nCheck that a VNC host is running there.", vex.Message),
                                string.Format("Unable to Connect to {0}", vncHost),
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                                string.Format("Unable to connect to host.  Error was: {0}", ex.Message),
                                string.Format("Unable to Connect to {0}", vncHost),
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);
            }
        }

        private void RemoteDesktop1OnConnectionLost(object sender, EventArgs eventArgs)
        {
            Dispatcher.Invoke(new Action(() => MessageBox.Show(this,
                string.Format("Lost Connection to host."),
                string.Format("Lost Connection to {0}", vncHost),
                MessageBoxButton.OK,
                MessageBoxImage.Exclamation)));
        }

        private void RemoteDesktop1OnConnectComplete(object sender, ConnectEventArgs connectEventArgs)
        {
            //remoteDesktop.Width = connectEventArgs.DesktopWidth;
            //remoteDesktop.Height = connectEventArgs.DesktopHeight;
            mainWindow.Title = connectEventArgs.DesktopName;
        }
    }
}
