using System;
using System.Windows.Forms;
using VncSharp;

namespace VncSharpTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            remoteDesktop1.ConnectComplete += RemoteDesktop1OnConnectComplete;
            remoteDesktop1.ConnectionLost += RemoteDesktop1OnConnectionLost;

            //var stream = new FileStream("example2.fbs", FileMode.Open);
            //remoteDesktop1.Connect(stream);
            remoteDesktop1.Connect("172.16.4.104");
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
