using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Yura
{
    public partial class SettingsWindow : Window
    {
        private const string ProgId = "Yura";
        private const string ProgFriendlyName = "Crystal Dynamics Bigfile";

        public SettingsWindow()
        {
            AvaloniaXamlLoader.Load(this);

            this.FindControl<ComboBox>("ThemeSelect")!.SelectedIndex = Settings.Default.Theme;
            this.FindControl<ComboBox>("SpecMaskViewSelect")!.SelectedIndex = Settings.Default.SpecMaskView;
            this.FindControl<ComboBox>("ClickActionSelect")!.SelectedIndex = Settings.Default.ClickAction;

            // Hide file associations button on non-Windows
            if (!OperatingSystem.IsWindows())
                this.FindControl<Button>("FileAssociationsBtn")!.IsVisible = false;

            Closed += (_, _) => Settings.Default.Save();
        }

        private void Theme_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var idx = this.FindControl<ComboBox>("ThemeSelect")!.SelectedIndex;
            Settings.Default.Theme = idx;
            (Avalonia.Application.Current as App)?.SetTheme((Theme)idx);
        }

        private void SpecMaskView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            Settings.Default.SpecMaskView = this.FindControl<ComboBox>("SpecMaskViewSelect")!.SelectedIndex;
        }

        private void ClickAction_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            Settings.Default.ClickAction = this.FindControl<ComboBox>("ClickActionSelect")!.SelectedIndex;
        }

        private void FileAssociations_Click(object? sender, RoutedEventArgs e)
        {
            if (!OperatingSystem.IsWindows())
            {
                _ = MessageBox.ShowDialog(this, "File associations are only supported on Windows.",
                    "Not supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
#if WINDOWS
                using var reg = Microsoft.Win32.Registry.ClassesRoot;

                reg.CreateSubKey(".000").SetValue(string.Empty, ProgId);
                reg.CreateSubKey(".dat").SetValue(string.Empty, ProgId);

                var program = reg.CreateSubKey(ProgId);
                program.SetValue(string.Empty, ProgFriendlyName);
                program.SetValue("FriendlyTypeName", ProgFriendlyName);

                var path = Process.GetCurrentProcess().MainModule!.FileName;
                program.CreateSubKey(@"shell\open\command").SetValue(string.Empty, $"\"{path}\" \"%1\"");

                SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
#endif

                _ = MessageBox.ShowDialog(this, "Yura will now open .000, .tiger and .dat files.",
                    "File associations set", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (UnauthorizedAccessException)
            {
                _ = MessageBox.ShowDialog(this, "Yura must be run as administrator to set file associations.",
                    "Missing access", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

#if WINDOWS
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
#endif
    }
}
