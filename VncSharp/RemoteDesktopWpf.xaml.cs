using System;
using System.Drawing;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;

using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;

namespace VncSharp
{
    [ToolboxBitmap(typeof(RemoteDesktopWpf), "Resources.vncviewer.ico")]

    public partial class RemoteDesktopWpf : UserControl
    {
        [Description("Raised after a successful call to the Connect() method.")]
        public event ConnectCompleteHandler ConnectComplete;

        [Description("Raised when the VNC Host drops the connection.")]
        public event EventHandler ConnectionLost;

        [Description("Raised when the VNC Host sends text to the client's clipboard.")]
        public event EventHandler ClipboardChanged;

        public event EventHandler StoppedListen;

        public AuthenticateDelegate GetPassword;

        WriteableBitmap _desktop;
        VncClient _vnc;
        int _port = 5900;
        bool _passwordPending;
        bool _fullScreenRefresh;
        VncDesktopTransformPolicy _desktopPolicy;
        RuntimeState _state = RuntimeState.Disconnected;

        double _imageScale;        // Image Scale

        private enum RuntimeState
        {
            Disconnected,
            Disconnecting,
            Connected,
            Connecting,
            Listen
        }

        public RemoteDesktopWpf()
        {
            InitializeComponent();

            // Use a simple desktop policy for design mode.  This will be replaced in Connect()
            _desktopPolicy = new VncDesignModeDesktopPolicy(this);

            if (_desktopPolicy.AutoScroll)
            {
                scrollviewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                scrollviewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                scrollviewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                scrollviewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

            // Users of the control can choose to use their own Authentication GetPassword() method via the delegate above.  This is a default only.
            GetPassword = PasswordDialogWpf.GetPassword;

            // EventHandler Settings
            designModeDesktop.SizeChanged += SizeChangedEventHandler;
            designModeDesktop.MouseMove += MouseDownUpMoveEventHandler;
            designModeDesktop.MouseDown += MouseDownUpMoveEventHandler;
            designModeDesktop.MouseUp += MouseDownUpMoveEventHandler;
            designModeDesktop.MouseWheel += MouseWHeelEventHandler;
        }

        private void SizeChangedEventHandler(object sender, RoutedEventArgs e)
        {
            if (IsConnected)
            {
                ImageScale = designModeDesktop.ActualWidth / designModeDesktop.Source.Width;
            }
        }

        public double ImageScale
        {
            get
            {
                return _imageScale;
            }
            set
            {
                _imageScale = value;
            }
        }

        [DefaultValue(5900)]
        [Description("The port number used by the VNC Host (typically 5900)")]
        public int VncPort
        {
            get
            {
                return _port;
            }
            set
            {
                // Ignore attempts to use invalid port numbers
                if (value < 1 | value > 65535) value = 5900;
                _port = value;
            }
        }

        public bool IsConnected
        {
            get
            {
                return _state == RuntimeState.Connected;
            }
        }

        public bool IsListen
        {
            get
            {
                return _state == RuntimeState.Listen;
            }
        }

        protected bool DesignMode
        {
            get
            {
                return DesignerProperties.GetIsInDesignMode(this);
            }
        }

        [Description("The name of the remote desktop.")]
        public string Hostname
        {
            get
            {
                return _vnc == null ? "Disconnected" : _vnc.HostName;
            }
        }

        public WriteableBitmap Desktop
        {
            get
            {
                return _desktop;
            }
        }

        public void FullScreenUpdate()
        {
            InsureConnection(true);
            _fullScreenRefresh = true;
        }

        private void InsureConnection(bool connected)
        {
            // Grab the name of the calling routine:
            string methodName = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name;

            if (connected)
            {
                System.Diagnostics.Debug.Assert(_state == RuntimeState.Connected ||
                                                _state == RuntimeState.Disconnecting,  // special case for Disconnect()
                                                string.Format("RemoteDesktop must be in RuntimeState.Connected before calling {0}.", methodName));
                if (_state != RuntimeState.Connected && _state != RuntimeState.Disconnecting)
                {
                    throw new InvalidOperationException("RemoteDesktop must be in Connected state before calling methods that require an established connection.");
                }
            }
            else
            { // disconnected
                System.Diagnostics.Debug.Assert(_state == RuntimeState.Disconnected ||
                                                _state == RuntimeState.Listen,
                                                string.Format("RemoteDesktop must be in RuntimeState.Disconnected before calling {0}.", methodName));
                if (_state != RuntimeState.Disconnected && _state != RuntimeState.Disconnecting && _state != RuntimeState.Listen)
                {
                    throw new InvalidOperationException("RemoteDesktop cannot be in Connected state when calling methods that establish a connection.");
                }
            }
        }

        private int updates;

        protected void VncUpdate(object sender, VncEventArgs e)
        {
            Dispatcher.Invoke(new Action(() => e.DesktopUpdater.Draw(_desktop)));

            if (_state == RuntimeState.Connected)
            {
                _vnc.RequestScreenUpdate(_fullScreenRefresh);

                // Make sure the next screen update is incremental
                _fullScreenRefresh = false;
            }
            updates++;
            if (updates%24 == 0)
            {
                updates = 0;
                _vnc.RequestScreenUpdate(true);
            }
        }

        public void Connect(string host)
        {
            // Use Display 0 by default.
            Connect(host, 0);
        }

        public void Connect(string host, bool viewOnly)
        {
            // Use Display 0 by default.
            Connect(host, 0, viewOnly);
        }

        public void Connect(string host, bool viewOnly, bool scaled)
        {
            // Use Display 0 by default.
            Connect(host, 0, viewOnly, scaled);
        }

        public void Connect(string host, int display)
        {
            Connect(host, display, false);
        }

        public void Connect(string host, int display, bool viewOnly)
        {
            Connect(host, display, viewOnly, false);
        }

        public void Connect(string host, int display, bool viewOnly, bool scaled)
        {
            // TODO: Should this be done asynchronously so as not to block the UI?  Since an event 
            // indicates the end of the connection, maybe that would be a better design.
            InsureConnection(false);

            if (host == null) throw new ArgumentNullException("host");
            if (display < 0) throw new ArgumentOutOfRangeException("display", display, "Display number must be a positive integer.");

            // Start protocol-level handling and determine whether a password is needed
            _vnc = new VncClient();
            _vnc.ConnectionLost += VncClientConnectionLost;
            _vnc.ServerCutText += VncServerCutText;

            _passwordPending = _vnc.Connect(host, display, VncPort, viewOnly);

            _desktopPolicy = new VncWpfDesktopPolicy(_vnc, this);
            SetScalingMode(scaled);

            if (_passwordPending)
            {
                // Server needs a password, so call which ever method is refered to by the GetPassword delegate.
                string password = GetPassword();

                if (password != null)
                {
                    Authenticate(password);
                }
            }
            else
            {
                // No password needed, so go ahead and Initialize here
                waitLabel.Content = "Connecting to VNC host " + host + "(" + _port + ") , please wait... ";
                Initialize();
            }
        }

        public void Connect(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            InsureConnection(false);

            _vnc = new VncClient();
            _vnc.ConnectionLost += VncClientConnectionLost;
            _vnc.ServerCutText += VncServerCutText;
            _vnc.Connect(stream, true);

            _desktopPolicy = new VncWpfDesktopPolicy(_vnc, this);

            SetScalingMode(true);

            waitLabel.Content = "Reading Stream from file , please wait... ";

            Initialize(true);
        }

        //public void Listen(string host, int port = 5500, bool viewOnly = false, bool scaled = false)
        //{
        //    InsureConnection(false);

        //    if (host == null)
        //    {
        //        throw new ArgumentNullException("host");
        //    }

        //    _vnc = new VncClient();
        //    _vnc.ConnectionLost += new EventHandler(VncClientConnectionLost);
        //    _vnc.ServerCutText += new EventHandler(VncServerCutText);
        //    _vnc.ConnectedFromServer += new VncConnectedFromServerHandler(ConnectedFromServerEventHandler);

        //    _desktopPolicy = new VncWpfDesktopPolicy(_vnc, this);
        //    SetScalingMode(scaled);
        //    SetState(RuntimeState.Listen);
        //    _vnc.Listen(host, port, viewOnly);

        //    this.waitLabel.Content = "Wait for a connection at " + Dns.GetHostEntry(host).AddressList[0] + ":" + port;
        //    this.waitLabel.Visibility = Visibility.Visible;
        //}

        public void ConnectedFromServerEventHandler(object sender, bool authentication)
        {
            if (authentication)
            {
                // Server needs a password, so call which ever method is refered to by the GetPassword delegate.
                var password = GetPassword();

                if (password != null)
                {
                    Authenticate(password);
                }
            }
            else
            {
                // No password needed, so go ahead and Initialize here
                Dispatcher.Invoke(new Action(() => Initialize()));
            }
        }

        public void Authenticate(string password)
        {
            InsureConnection(false);
            if (!_passwordPending) throw new InvalidOperationException("Authentication is only required when Connect() returns True and the VNC Host requires a password.");
            if (password == null) throw new NullReferenceException("password");

            _passwordPending = false;  // repeated calls to Authenticate should fail.
            if (_vnc.Authenticate(password))
            {
                Initialize();
            }
            else
            {
                OnConnectionLost();
            }
        }

        public void SetInputMode(bool viewOnly)
        {
            if (_vnc != null && IsConnected)
            {
                _vnc.SetInputMode(viewOnly);
            }
        }

        public void SetScalingMode(bool scaled)
        {
            if (scaled)
            {
                scrollviewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                scrollviewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                scrollviewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                scrollviewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }
        }

        protected void Initialize(bool streaming = false)
        {
            // Finish protocol handshake with host now that authentication is done.
            InsureConnection(false);
            _vnc.Initialize(streaming);
            SetState(RuntimeState.Connected);

            // Create a buffer on which updated rectangles will be drawn and draw a "please wait..." 
            // message on the buffer for initial display until we start getting rectangles
            SetupDesktop();

            // Set Ket Event Handler
            scrollviewer.PreviewKeyDown += KeyDownEventHandler;
            scrollviewer.PreviewKeyUp += KeyUpEventHandler;
            scrollviewer.Focus();

            // Tell the user of this control the necessary info about the desktop in order to setup the display
            OnConnectComplete(new ConnectEventArgs(_vnc.Framebuffer.Width,
                                                   _vnc.Framebuffer.Height,
                                                   _vnc.Framebuffer.DesktopName));

            // Start getting updates from the remote host (vnc.StartUpdates will begin a worker thread).
            _vnc.VncUpdate += VncUpdate;
            _vnc.StartUpdates(streaming);
        }

        private void SetState(RuntimeState newState)
        {
            _state = newState;

            // Set mouse pointer according to new state
            switch (_state)
            {
                case RuntimeState.Connected:
                    // Change the cursor to the "vnc" custor--a see-through dot
                    //Cursor = new Cursor(GetType(), "Resources.vnccursor.cur");
                    Dispatcher.Invoke(new Action(() =>
                    {
                        //Cursor = new Cursor("VncSharpWpf.Resources.vnccursor.cur");
                        Cursor = ((TextBlock)this.Resources["VncCursor"]).Cursor;
                    }));
                    break;
                // All other states should use the normal cursor.
                case RuntimeState.Disconnected:
                default:
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Cursor = Cursors.Arrow;
                    }));
                    break;
            }
        }

        protected void SetupDesktop()
        {
            InsureConnection(true);


            var colors = new List<System.Windows.Media.Color>
            {
                System.Windows.Media.Colors.Red,
                System.Windows.Media.Colors.Blue,
                System.Windows.Media.Colors.Green
            };
            var myPalette = new BitmapPalette(colors);

            Dispatcher.Invoke(new Action(() =>
            {
                _desktop = new WriteableBitmap(_vnc.Framebuffer.Width, _vnc.Framebuffer.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32, myPalette);
                designModeDesktop.Source = _desktop;
            }));

            waitLabel.Visibility = Visibility.Visible;
        }

        public void Disconnect()
        {
            InsureConnection(true);
            _vnc.ConnectionLost -= VncClientConnectionLost;
            _vnc.ServerCutText -= VncServerCutText;
            _vnc.Disconnect();

            scrollviewer.PreviewKeyDown -= KeyDownEventHandler;
            scrollviewer.PreviewKeyUp -= KeyUpEventHandler;

            if (designModeDesktop.Dispatcher.CheckAccess())
            {
                designModeDesktop.Source = null;
                waitLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    designModeDesktop.Source = null;
                    waitLabel.Visibility = Visibility.Hidden;
                }));
            }

            SetState(RuntimeState.Disconnected);
            OnConnectionLost();
        }

        //public void StopListen()
        //{
        //    InsureConnection(false);

        //    _vnc.ConnectionLost -= new EventHandler(VncClientConnectionLost);
        //    _vnc.ServerCutText -= new EventHandler(VncServerCutText);
        //    _vnc.ConnectedFromServer -= new VncConnectedFromServerHandler(ConnectedFromServerEventHandler);
        //    _vnc.StopListen();

        //    this.scrollviewer.PreviewKeyDown -= new KeyEventHandler(KeyDownEventHandler);
        //    this.scrollviewer.PreviewKeyUp -= new KeyEventHandler(KeyUpEventHandler);

        //    this.waitLabel.Visibility = Visibility.Hidden;
        //    SetState(RuntimeState.Disconnected);

        //    OnStoppedListen();
        //}

        public void FillServerClipboard()
        {
            FillServerClipboard(Clipboard.GetText());
        }

        public void FillServerClipboard(string text)
        {
            _vnc.WriteClientCutText(text);
        }

        protected void VncClientConnectionLost(object sender, EventArgs e)
        {
            // If the remote host dies, and there are attempts to write
            // keyboard/mouse/update notifications, this may get called 
            // many times, and from main or worker thread.
            // Guard against this and invoke Disconnect once.
            if (_state == RuntimeState.Connected)
            {
                SetState(RuntimeState.Disconnecting);
                Disconnect();
            }
            //else if (_state == RuntimeState.Listen)
            //{
            //    StopListen();
            //}
        }

        protected void VncServerCutText(object sender, EventArgs e)
        {
            OnClipboardChanged();
        }

        protected void OnClipboardChanged()
        {
            if (ClipboardChanged != null)
                ClipboardChanged(this, EventArgs.Empty);
        }

        protected void OnConnectionLost()
        {
            if (ConnectionLost != null)
            {
                ConnectionLost(this, EventArgs.Empty);
            }
        }

        protected void OnConnectComplete(ConnectEventArgs e)
        {
            if (ConnectComplete != null)
            {
                ConnectComplete(this, e);
            }
        }

        protected void OnStoppedListen()
        {
            if (StoppedListen != null)
            {
                StoppedListen(this, EventArgs.Empty);
            }
        }

        private void MouseDownUpMoveEventHandler(object sender, MouseEventArgs e)
        {
            // Only bother if the control is connected.
            if (IsConnected)
            {
                UpdateRemotePointer();
            }
        }

        private void MouseWHeelEventHandler(object sender, MouseWheelEventArgs e)
        {
            if (!DesignMode && IsConnected)
            {
                System.Windows.Point mousePoint = Mouse.GetPosition(designModeDesktop);
                var current = new System.Drawing.Point(Convert.ToInt32(mousePoint.X), Convert.ToInt32(mousePoint.Y));

                byte mask = 0;

                // mouse was scrolled forward
                if (e.Delta > 0)
                {
                    mask += 8;
                }
                else if (e.Delta < 0)
                { // mouse was scrolled backwards
                    mask += 16;
                }

                _vnc.WritePointerEvent(mask, _desktopPolicy.GetMouseMovePoint(current));
            }
        }

        private void UpdateRemotePointer()
        {
            // HACK: this check insures that while in DesignMode, no messages are sent to a VNC Host
            // (i.e., there won't be one--NullReferenceException)			
            if (!DesignMode && IsConnected)
            {
                System.Windows.Point mousePoint = Mouse.GetPosition(designModeDesktop);
                var current = new System.Drawing.Point(Convert.ToInt32(mousePoint.X), Convert.ToInt32(mousePoint.Y));

                byte mask = 0;

                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    mask += 1;
                }

                if (Mouse.MiddleButton == MouseButtonState.Pressed)
                {
                    mask += 2;
                }

                if (Mouse.RightButton == MouseButtonState.Pressed)
                {
                    mask += 4;
                }

                System.Drawing.Point adjusted = _desktopPolicy.UpdateRemotePointer(current);
                //if (adjusted.X < 0 || adjusted.Y < 0)
                //    throw new Exception();

                _vnc.WritePointerEvent(mask, _desktopPolicy.UpdateRemotePointer(current));
            }
        }

        private void KeyDownEventHandler(object sender, KeyEventArgs e)
        {
            if (DesignMode || !IsConnected)
                return;

            ManageKeyDownAndKeyUp(e, true);

            char ascii = ConvertKeyToAscii(e.Key);

            if (Char.IsLetterOrDigit(ascii) || Char.IsWhiteSpace(ascii) || Char.IsPunctuation(ascii) ||
                ascii == '~' || ascii == '`' || ascii == '<' || ascii == '>' || ascii == '|' ||
                ascii == '=' || ascii == '+' || ascii == '$' || ascii == '^')
            {
                _vnc.WriteKeyboardEvent(ascii, true);
                _vnc.WriteKeyboardEvent(ascii, false);
            }
            else if (ascii == '\b')
            {
                const uint keyChar = ((UInt32)'\b') | 0x0000FF00;
                _vnc.WriteKeyboardEvent(keyChar, true);
                _vnc.WriteKeyboardEvent(keyChar, false);
            }

            e.Handled = true;
        }

        private void KeyUpEventHandler(object sender, KeyEventArgs e)
        {
            if (DesignMode || !IsConnected)
                return;

            ManageKeyDownAndKeyUp(e, false);

            e.Handled = true;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        internal static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

        [DllImport("user32.dll")]
        static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
                                      [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff,
                                      int cchBuff, uint wFlags, IntPtr dwhkl);

        /// <summary>
        /// Convert Key to Ascii Character.
        /// </summary>
        /// <param name="key"> Source Key </param>
        private char ConvertKeyToAscii(Key key)
        {
            var keyState = new byte[256];
            GetKeyboardState(keyState);

            var vKey = (uint)KeyInterop.VirtualKeyFromKey(key);
            IntPtr kbLayout = GetKeyboardLayout(0);
            uint scanCode = MapVirtualKeyEx(vKey, 0, kbLayout);

            var sb = new StringBuilder();
            int result = ToUnicodeEx(vKey, scanCode, keyState, sb, 5, 0, kbLayout);

            switch (result)
            {
                default:
                case -1:
                case 0:
                    return (char)0;
                case 1:
                case 2:
                    return sb.ToString()[0];
            }
        }

        // Thanks to Lionel Cuir, Christian and the other developers at 
        // Aulofee.com for cleaning-up my keyboard code, specifically:
        // ManageKeyDownAndKeyUp, OnKeyPress, OnKeyUp, OnKeyDown.
        private void ManageKeyDownAndKeyUp(KeyEventArgs e, bool isDown)
        {
            UInt32 keyChar;
            bool isProcessed = true;
            switch (e.Key)
            {
                case Key.Tab: keyChar = 0x0000FF09; break;
                case Key.Enter: keyChar = 0x0000FF0D; break;
                case Key.Escape: keyChar = 0x0000FF1B; break;
                case Key.Home: keyChar = 0x0000FF50; break;
                case Key.Left: keyChar = 0x0000FF51; break;
                case Key.Up: keyChar = 0x0000FF52; break;
                case Key.Right: keyChar = 0x0000FF53; break;
                case Key.Down: keyChar = 0x0000FF54; break;
                case Key.PageUp: keyChar = 0x0000FF55; break;
                case Key.PageDown: keyChar = 0x0000FF56; break;
                case Key.End: keyChar = 0x0000FF57; break;
                case Key.Insert: keyChar = 0x0000FF63; break;
                case Key.LeftShift: keyChar = 0x0000FFE1; break;
                case Key.RightShift: keyChar = 0x0000FFE1; break;

                // BUG FIX -- added proper Alt/CTRL support (Edward Cooke)
                case Key.LeftAlt: keyChar = 0x0000FFE9; break;
                case Key.RightAlt: keyChar = 0x0000FFE9; break;
                case Key.LeftCtrl: keyChar = 0x0000FFE3; break;
                case Key.RightCtrl: keyChar = 0x0000FFE4; break;

                case Key.Delete: keyChar = 0x0000FFFF; break;
                case Key.LWin: keyChar = 0x0000FFEB; break;
                case Key.RWin: keyChar = 0x0000FFEC; break;
                case Key.Apps: keyChar = 0x0000FFEE; break;
                case Key.F1:
                case Key.F2:
                case Key.F3:
                case Key.F4:
                case Key.F5:
                case Key.F6:
                case Key.F7:
                case Key.F8:
                case Key.F9:
                case Key.F10:
                case Key.F11:
                case Key.F12:
                    keyChar = 0x0000FFBE + ((UInt32)e.Key - (UInt32)Key.F1);
                    break;
                default:
                    keyChar = 0;
                    isProcessed = false;
                    break;
            }

            if (isProcessed)
            {
                _vnc.WriteKeyboardEvent(keyChar, isDown);
                e.Handled = true;
            }
        }

        public void SendSpecialKeys(SpecialKeys keys)
        {
            SendSpecialKeys(keys, true);
        }

        public void SendSpecialKeys(SpecialKeys keys, bool release)
        {
            InsureConnection(true);
            // For all of these I am sending the key presses manually instead of calling
            // the keyboard event handlers, as I don't want to propegate the calls up to the 
            // base control class and form.
            switch (keys)
            {
                case SpecialKeys.Ctrl:
                    PressKeys(new uint[] { 0xffe3 }, release);	// CTRL, but don't release
                    break;
                case SpecialKeys.Alt:
                    PressKeys(new uint[] { 0xffe9 }, release);	// ALT, but don't release
                    break;
                case SpecialKeys.CtrlAltDel:
                    PressKeys(new uint[] { 0xffe3, 0xffe9, 0xffff }, release); // CTRL, ALT, DEL
                    break;
                case SpecialKeys.AltF4:
                    PressKeys(new uint[] { 0xffe9, 0xffc1 }, release); // ALT, F4
                    break;
                case SpecialKeys.CtrlEsc:
                    PressKeys(new uint[] { 0xffe3, 0xff1b }, release); // CTRL, ESC
                    break;
                // TODO: are there more I should support???
                default:
                    break;
            }
        }

        private void PressKeys(uint[] keys, bool release)
        {
            System.Diagnostics.Debug.Assert(keys != null, "keys[] cannot be null.");

            foreach (uint t in keys)
            {
                _vnc.WriteKeyboardEvent(t, true);
            }

            if (release)
            {
                // Walk the keys array backwards in order to release keys in correct order
                for (int i = keys.Length - 1; i >= 0; --i)
                {
                    _vnc.WriteKeyboardEvent(keys[i], false);
                }
            }
        }
    }
}