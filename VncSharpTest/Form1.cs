using System;
using System.IO;
using System.Windows.Forms;
using VncSharp;

namespace VncSharpTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            var stream = new FileStream("example2.fbs", FileMode.Open);
            remoteDesktop1.ConnectComplete += RemoteDesktop1OnConnectComplete;
            remoteDesktop1.ConnectionLost += RemoteDesktop1OnConnectionLost;
            remoteDesktop1.Connect(stream);
        }

        private void RemoteDesktop1OnConnectionLost(object sender, EventArgs eventArgs)
        {
        }

        private void RemoteDesktop1OnConnectComplete(object sender, ConnectEventArgs connectEventArgs)
        {
            Width = connectEventArgs.DesktopWidth + 20;
            Height = connectEventArgs.DesktopHeight + 50;
            Text += ": " + connectEventArgs.DesktopName;
        }
    }
}
