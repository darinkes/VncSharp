using System.Windows;
using System.Windows.Input;

namespace VncSharp
{

    public partial class PasswordDialogWpf : Window
    {
        public PasswordDialogWpf()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the Password entered by the user.
        /// </summary>
        public string Password
        {
            get
            {
                return this.passwordBox.Password;
            }
        }

        /// <summary>
        /// Creates an instance of PasswordDialog and uses it to obtain the user's password.
        /// </summary>
        /// <returns>Returns the user's password as entered, or null if he/she clicked Cancel.</returns>
        public static string GetPassword()
        {
            PasswordDialogWpf dialog = new PasswordDialogWpf();

            dialog.passwordBox.Focus();
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                return dialog.passwordBox.Password;
            }
            else
            {
                return null;
            }
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void PasswordBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.passwordBox.Password.Length > 0)
            {
                if (e.Key == Key.Enter)
                {
                    // If Enter Key is Pressed and Password length is not 0, Password is accepted.
                    this.DialogResult = true;
                }
                else
                {
                    this.OK_Button.IsEnabled = true;
                }
            }
            else
            {
                this.OK_Button.IsEnabled = false;
            }
        }
    }
}