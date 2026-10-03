using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.IO;
using System.Linq;
using Yura.Formats;
using Yura.Shared.IO;

namespace Yura
{
    public partial class LocaleViewer : Window
    {
        private LocaleFile _locale = null!;

        public LocaleViewer() { AvaloniaXamlLoader.Load(this); }

        public Endianness Endianness { get; set; }

        public byte[] Data
        {
            set
            {
                _locale = new LocaleFile(value, Endianness);
                Title += " - " + _locale.Language;
                this.FindControl<ListBox>("Entries")!.ItemsSource =
                    _locale.Entries.Select((x, i) => new LocaleViewerEntry { Index = i, Value = x }).ToList();
            }
        }

        private async void ExportCommand_Click(object? sender, RoutedEventArgs e)
        {
            var path = await FilePicker.SaveFile(this, "Save", "locals.txt",
                ("Text Files", new[] { "*.txt" }), ("All Files", new[] { "*.*" }));
            if (path == null) return;

            File.WriteAllLines(path, _locale.Entries.Select((x, i) => $"{i} = {x}\n"));
        }

        private class LocaleViewerEntry
        {
            public int Index { get; set; }
            public string Value { get; set; } = "";
        }
    }
}
