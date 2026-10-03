using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Yura
{
    public enum MessageBoxButtons { OK, OKCancel }
    public enum MessageBoxIcon { None, Information, Warning, Error, Exclamation }
    public enum MessageBoxResult { OK, Cancel }

    public partial class MessageBox : Window
    {
        private MessageBoxResult _result = MessageBoxResult.Cancel;

        public MessageBox() { InitializeComponent(); }

        private void InitializeComponent() => Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);

        private void OkBtn_Click(object? sender, RoutedEventArgs e)     { _result = MessageBoxResult.OK;     Close(_result); }
        private void CancelBtn_Click(object? sender, RoutedEventArgs e) { _result = MessageBoxResult.Cancel; Close(_result); }

        public static async System.Threading.Tasks.Task<MessageBoxResult> ShowDialog(
            Window? owner, string message, string title,
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.None)
        {
            var box = new MessageBox { Title = title };
            box.FindControl<TextBlock>("MessageText")!.Text = message;
            box.FindControl<Button>("CancelBtn")!.IsVisible = buttons == MessageBoxButtons.OKCancel;

            if (owner != null)
                return await box.ShowDialog<MessageBoxResult>(owner);
            box.Show();
            return MessageBoxResult.OK;
        }

        public static void Show(Window? owner, string message, string title,
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.None)
        {
            _ = ShowDialog(owner, message, title, buttons, icon);
        }
    }
}
