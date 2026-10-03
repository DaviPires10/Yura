using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Yura.Formats;
using Yura.Shared.Archive;
using Yura.Shared.IO;

namespace Yura
{
    public partial class SearchWindow : Window
    {
        public ArchiveFile? Archive { get; set; }
        public Endianness Endianness { get; set; }

        public SearchWindow() { AvaloniaXamlLoader.Load(this); }

        private void SearchButton_Click(object? sender, RoutedEventArgs e)
        {
            if (Archive == null)
            {
                _ = MessageBox.ShowDialog(this, Properties.Resources.NoBigfileOpenMessage2,
                    Properties.Resources.NoBigfileOpen, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var idText = this.FindControl<TextBox>("IdField")!.Text;
            if (!int.TryParse(idText, out var id))
            {
                _ = MessageBox.ShowDialog(this, "Entered ID must be a number.", "Invalid ID",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var files = Archive.Records.Where(x => x.Name != null && x.Name.EndsWith(".drm")).ToList();

            var sectionType = this.FindControl<ComboBox>("TypeSelect")!.SelectedIndex switch
            {
                0 => SectionType.Texture,
                1 => SectionType.Wave,
                2 => SectionType.Animation,
                3 => SectionType.Dtp,
                _ => SectionType.Texture,
            };

            var results = this.FindControl<ListBox>("SearchResults")!;
            var progress = this.FindControl<ProgressBar>("Progress")!;
            results.ItemsSource = new List<SearchResult>();
            progress.Value = 0;
            progress.Maximum = files.Count;

            Task.Run(() => SearchTask(files, id, sectionType));
        }

        private void SearchResults_DoubleTapped(object? sender, TappedEventArgs e)
        {
            var list = this.FindControl<ListBox>("SearchResults")!;
            if (list.SelectedItem is not SearchResult item) return;

            var filename = Path.GetFileName(item.File);
            var path = Path.GetDirectoryName(item.File);

            if (Owner is MainWindow mw)
                mw.SwitchDirectory(path, filename);
        }

        private void SearchTask(List<ArchiveRecord> files, int id, SectionType type)
        {
            foreach (var file in files)
            {
                byte[] content;
                try { content = Archive!.Read(file); }
                catch (FileNotFoundException)
                {
                    Dispatcher.UIThread.Post(() => _ = MessageBox.ShowDialog(this,
                        Properties.Resources.FilePartNotFoundMessage, Properties.Resources.FilePartNotFound,
                        MessageBoxButtons.OK, MessageBoxIcon.Error));
                    break;
                }

                if (BitConverter.ToUInt32(content) != 14)
                {
                    Dispatcher.UIThread.Post(() => _ = MessageBox.ShowDialog(this,
                        Properties.Resources.DeepSearchNotSupported, Properties.Resources.GameNotSupported,
                        MessageBoxButtons.OK, MessageBoxIcon.Error));
                    break;
                }

                var drm = new DrmFile(content, Endianness);
                foreach (var section in drm.Sections)
                {
                    if (section.Type == type && section.Id == id)
                    {
                        var result = new SearchResult { File = file.Name!, Section = section.Index };
                        Dispatcher.UIThread.Post(() =>
                        {
                            var list = this.FindControl<ListBox>("SearchResults")!;
                            var items = (list.ItemsSource as List<SearchResult>) ?? new();
                            items.Add(result);
                            list.ItemsSource = items.ToList();
                        });
                    }
                }

                Dispatcher.UIThread.Post(() =>
                    this.FindControl<ProgressBar>("Progress")!.Value++);
            }
        }

        private class SearchResult
        {
            public string File { get; set; } = "";
            public int Section { get; set; }
        }
    }
}
