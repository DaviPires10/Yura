using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yura.Formats;
using Yura.Shared.Archive;
using Yura.Shared.IO;
using Yura.Shared.Util;

namespace Yura
{
    public partial class MainWindow : Window
    {
        private Bitmap _folderIcon;
        private Bitmap _archiveIcon;
        private Bitmap _stackIcon;
        private Bitmap _binaryIcon;
        private Bitmap _classIcon;
        private Bitmap _imageIcon;
        private Bitmap _soundIcon;
        private Bitmap _textIcon;

        private Dictionary<string, (string name, Bitmap icon)> _fileTypes;

        private Endianness _endianness;
        private Platform _platform;
        private Game _currentGame;
        private ArchiveFile _bigfile = null!;
        private string _sortColumn = "Name";
        private bool _sortAscending = true;

        public MainWindow()
        {
            InitializeComponent();

            _folderIcon  = LoadIcon("FolderClosed.png");
            _archiveIcon = LoadIcon("ZipFile.png");
            _stackIcon   = LoadIcon("ImageStack.png");
            _binaryIcon  = LoadIcon("BinaryFile.png");
            _classIcon   = LoadIcon("ClassFile.png");
            _imageIcon   = LoadIcon("Image.png");
            _soundIcon   = LoadIcon("SoundFile.png");
            _textIcon    = LoadIcon("TextFile.png");

            _fileTypes = new()
            {
                { ".drm",  ("Data Ram", _classIcon) },
                { ".vrm",  ("Video Ram", _stackIcon) },
                { ".mul",  ("MultiplexStream", _soundIcon) },
                { ".raw",  ("RAW Image", _imageIcon) },
                { ".mus",  ("Music", _soundIcon) },
                { ".sam",  ("Music Sample", _soundIcon) },
                { ".ids",  ("IDMap", _textIcon) },
                { ".sch",  ("SchemaFile", _textIcon) },
                { ".tfb",  ("PadShock Library", _binaryIcon) },
                { ".txt",  ("Text File", _textIcon) },
                { ".ini",  ("Text File", _textIcon) },
                { ".json", ("JavaScript Object Notation", _textIcon) },
                { ".csv",  ("Comma Separated Values", _textIcon) },
                { ".ico",  ("Icon", _imageIcon) },
                { ".png",  ("Portable Network Graphics", _imageIcon) },
                { ".bnk",  ("Wwise SoundBank", _soundIcon) },
                { ".wem",  ("Wwise Encoded Media", _soundIcon) },
                { ".tpl",  ("Texture Palette Library", _imageIcon) },
                { ".arc",  ("GameCube Archive", _archiveIcon) },
                { ".brsar",("Binary Revolution Sound Archive", _archiveIcon) },
            };
        }

        private static Bitmap LoadIcon(string name) =>
            new Bitmap(AssetLoader.Open(new Uri($"avares://Yura/Images/{name}")));

        public ArchiveFile Bigfile => _bigfile;
        public Game Game => _currentGame;

        // ---- open ----

        public async void OpenBigfileDialog(string bigfile)
        {
            var dialog = new OpenDialog();
            if (Path.GetExtension(bigfile) == ".tiger")
                dialog.Game = Game.Tiger;

            var result = await dialog.ShowDialog<bool>(this);
            if (result)
                OpenBigfile(bigfile, dialog);
        }

        public void OpenBigfile(string bigfile, IFileSettings settings)
        {
            var list = settings.FileList == null
                ? null
                : new FileList(settings.FileList, settings.Game != Game.Tiger);

            _endianness = settings.Endianness;
            _platform = settings.Platform;
            _currentGame = settings.Game;

            var options = new ArchiveOptions
            {
                Path = bigfile,
                Endianness = settings.Endianness,
                Platform = settings.Platform,
                Alignment = settings.Alignment,
                FileList = list
            };

            switch (settings.Game)
            {
                case Game.Legend:   _bigfile = new LegendArchive(options); break;
                case Game.DeusEx:   _bigfile = new DeusExArchive(options); break;
                case Game.Defiance: _bigfile = new DefianceArchive(options); break;
                case Game.Tiger:    _bigfile = new TigerArchive(options); break;
                default:
                    _ = MessageBox.ShowDialog(this, Properties.Resources.NoGameSelectedMessage,
                        Properties.Resources.NoGameSelected, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
            }

            this.FindControl<TextBox>("PathBox")!.Text = Path.GetFileName(bigfile);

            try { _bigfile.Open(); }
            catch (Exception e)
            {
                _ = MessageBox.ShowDialog(this, e.Message, Properties.Resources.FailedOpenBigfile,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            list?.Resolve(_bigfile.Records);
            UpdateTree();
        }

        // ---- tree ----

        private void UpdateTree()
        {
            var root = new DirectoryViewFolder { Name = "Bigfile", Image = _archiveIcon, Subfolders = new() };

            foreach (var file in _bigfile.Records)
            {
                if (file.Name == null) continue;

                var hierarchy = file.Name.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                var parent = root;
                int i = 1;
                foreach (var sub in hierarchy.Take(hierarchy.Length - 1))
                {
                    var path = hierarchy.Take(i).ToArray();
                    parent = parent.TryCreateFolder(sub, path, _folderIcon);
                    i++;
                }
            }

            var tree = this.FindControl<TreeView>("DirectoryView")!;
            tree.ItemsSource = new List<DirectoryViewFolder> { root };
        }

        private void DirectoryView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var folder = (this.FindControl<TreeView>("DirectoryView")!.SelectedItem) as DirectoryViewFolder;
            SwitchDirectory(folder?.Path);
        }

        public void SwitchDirectory(string? path, string? selectedFile = null)
        {
            var files = GetFiles(path);
            var bigfile = Path.GetFileName(_bigfile.Name);

            this.FindControl<TextBox>("PathBox")!.Text =
                path == null ? bigfile : bigfile + "\\" + path;

            ShowFiles(files, selectedFile);
        }

        private List<ArchiveRecord> GetFiles(string? path)
        {
            if (path == null)
                return _bigfile.Records.Where(x => x.Name == null).ToList();
            return _bigfile.GetFiles(path);
        }

        private void ShowFiles(List<ArchiveRecord> files, string? selectedFile = null)
        {
            var view = new List<FileViewFile>();
            FileViewFile? selected = null;

            foreach (var file in files)
            {
                var type = GetFileType(Path.GetExtension(file.Name ?? ""));
                string name = file.Name == null
                    ? file.Hash.ToString("X")
                    : file.Name.Split(new[] { '\\', '/' }).Last();

                var v = new FileViewFile
                {
                    Name = name,
                    Type = type.name,
                    Size = file.Size,
                    Image = type.icon,
                    File = file
                };

                if (_currentGame >= Game.Legend)
                    v.SpecMask = GetSpecMask(file);

                if (name == selectedFile) selected = v;
                view.Add(v);
            }

            var list = this.FindControl<ListBox>("FileView")!;
            list.ItemsSource = view;

            if (selected != null)
            {
                list.SelectedItem = selected;
                list.ScrollIntoView(selected);
            }
        }

        private string GetSpecMask(ArchiveRecord record)
        {
            var specMask = (uint)record.Specialisation;
            switch ((SpecMaskView)Settings.Default.SpecMaskView)
            {
                default:
                case SpecMaskView.Flags:
                    specMask &= ~(uint)0x7fffffe0;
                    return ((SpecialisationFlags)specMask).ToString();
                case SpecMaskView.Bits:
                    specMask &= ~(uint)0x7fffffe0;
                    return GetBinaryRepresentation(specMask);
                case SpecMaskView.Hex:
                    return specMask.ToString("X");
            }
        }

        private static string GetBinaryRepresentation(uint number)
        {
            var sb = new StringBuilder();
            for (int i = 31; i >= 0; i--)
                sb.Append((number & (1 << i)) != 0 ? '1' : '0');
            return sb.ToString();
        }

        private (string name, Bitmap icon) GetFileType(string ext)
            => _fileTypes.TryGetValue(ext, out var t) ? t : ("File", _binaryIcon);

        // ---- commands / handlers ----

        private async void OpenCommand_Click(object? sender, RoutedEventArgs e)
        {
            var file = await FilePicker.OpenFile(this, "Select bigfile",
                ("Bigfile", new[] { "*.000", "*.dat", "*.000.wii-w", "*.000.tiger" }));
            if (file != null) OpenBigfileDialog(file);
        }

        private void CloseCommand_Click(object? sender, RoutedEventArgs e) => Close();

        private void SearchCommand_Click(object? sender, RoutedEventArgs e)
        {
            var w = new SearchWindow { Archive = _bigfile, Endianness = _endianness };
            w.Show(this);
        }

        private void SettingsCommand_Click(object? sender, RoutedEventArgs e)
        {
            var w = new SettingsWindow();
            w.ShowDialog(this);
        }

        private async void CopyFileList_Click(object? sender, RoutedEventArgs e)
        {
            if (_bigfile == null) return;

            var sb = new StringBuilder();
            foreach (var f in _bigfile.Records)
                sb.AppendLine($"{f.Hash:X8}\t{f.Size}\t{(uint)f.Specialisation:X}\t{f.Name}");

            var clip = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clip != null) await clip.SetTextAsync(sb.ToString());
        }

        private void SearchBox_GotFocus(object? sender, GotFocusEventArgs e)
        {
            this.FindControl<TextBox>("SearchBox")!.Text = "";
        }

        private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var search = this.FindControl<TextBox>("SearchBox")!;
            var query = search.Text?.ToLower() ?? "";

            if (_bigfile == null)
            {
                _ = MessageBox.ShowDialog(this, Properties.Resources.NoBigfileOpenMessage,
                    Properties.Resources.NoBigfileOpen, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var results = _bigfile.Records.Where(r =>
            {
                string fn = r.Name != null
                    ? r.Name.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).Last()
                    : r.Hash.ToString("X");
                return fn.ToLower().Contains(query);
            }).ToList();

            ShowFiles(results);
            this.FindControl<TextBox>("PathBox")!.Text = "Search results for " + query;
        }

        private void Header_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button b || b.Tag is not string col) return;

            if (_sortColumn == col) _sortAscending = !_sortAscending;
            else { _sortColumn = col; _sortAscending = true; }

            var list = this.FindControl<ListBox>("FileView")!;
            var items = list.ItemsSource as List<FileViewFile>;
            if (items == null) return;

            IEnumerable<FileViewFile> sorted = _sortColumn switch
            {
                "Name"     => items.OrderBy(x => x.Name),
                "Type"     => items.OrderBy(x => x.Type),
                "Size"     => items.OrderBy(x => x.Size),
                "SpecMask" => items.OrderBy(x => x.SpecMask),
                _ => items
            };
            if (!_sortAscending) sorted = sorted.Reverse();
            list.ItemsSource = sorted.ToList();
        }

        private async void FileView_DoubleTapped(object? sender, TappedEventArgs e)
        {
            var list = this.FindControl<ListBox>("FileView")!;
            if (list.SelectedItem is not FileViewFile item) return;
            var clickAction = (DoubleClickAction)Settings.Default.ClickAction;

            if (clickAction == DoubleClickAction.Export) { await ExportFile(item); return; }

            byte[] data;
            try { data = _bigfile.Read(item.File); }
            catch (Exception ex) when (ex is FileNotFoundException || ex is EndOfStreamException)
            {
                _ = MessageBox.ShowDialog(this, Properties.Resources.FilePartNotFoundMessage,
                    Properties.Resources.FilePartNotFound, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // RAW magic → texture viewer
            if (data.Length > 4 && data[0] == 33 && data[1] == 'W' && data[2] == 'A' && data[3] == 'R')
            {
                var viewer = new TextureViewer { Platform = _platform, Endianness = _endianness };
                viewer.Texture = data;
                viewer.Title = item.Name;
                viewer.Show();
                return;
            }

            if (item.Name == "locals.bin")
            {
                var viewer = new LocaleViewer { Endianness = _endianness };
                viewer.Data = data;
                viewer.Show();
                return;
            }

            if (clickAction == DoubleClickAction.PreviewFile) { await ExportFile(item); return; }

            // Fallback: write to temp and open with shell
            var path = Path.Combine(Path.GetTempPath(), "Yura", item.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            try
            {
                File.WriteAllBytes(path, data);
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (IOException ex)
            {
                _ = MessageBox.ShowDialog(this, ex.Message, Properties.Resources.FailedWriteFile,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void ExportBtn_Click(object? sender, RoutedEventArgs e)
        {
            var list = this.FindControl<ListBox>("FileView")!;
            var selected = list.SelectedItems?.Cast<FileViewFile>().ToList() ?? new();
            if (selected.Count == 0) return;

            if (selected.Count > 1)
            {
                var folder = await FilePicker.OpenFolder(this, "Select export folder");
                if (folder == null) return;

                foreach (var item in selected)
                {
                    var path = Path.Combine(folder, item.Name);
                    try
                    {
                        var file = _bigfile.Read(item.File);
                        await File.WriteAllBytesAsync(path, file);
                    }
                    catch (Exception ex)
                    {
                        _ = MessageBox.ShowDialog(this, ex.Message, Properties.Resources.FailedExportFiles,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    }
                }
            }
            else
            {
                await ExportFile(selected[0]);
            }
        }

        private async Task ExportFile(FileViewFile item)
        {
            var ext = Path.GetExtension(item.Name);
            var filters = item.Type == "File"
                ? new (string, string[])[] { ("All Files", new[] { "*.*" }) }
                : new (string, string[])[] { (item.Type, new[] { "*" + ext }), ("All Files", new[] { "*.*" }) };

            var path = await FilePicker.SaveFile(this, "Export", item.Name, filters);
            if (path == null) return;

            try
            {
                var file = _bigfile.Read(item.File);
                await File.WriteAllBytesAsync(path, file);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is EndOfStreamException)
            {
                _ = MessageBox.ShowDialog(this, Properties.Resources.FilePartNotFoundMessage,
                    Properties.Resources.FilePartNotFound, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                _ = MessageBox.ShowDialog(this, ex.Message, Properties.Resources.FailedWriteFile,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    public class DirectoryViewFolder
    {
        public string Name { get; set; } = "";
        public string? Path { get; set; }
        public string[]? Parents { get; set; }
        public Bitmap? Image { get; set; }
        public ObservableCollection<DirectoryViewFolder> Subfolders { get; set; } = new();

        public DirectoryViewFolder TryCreateFolder(string name, string[] hierarchy, Bitmap icon)
        {
            var sub = Subfolders.FirstOrDefault(x => x.Name == name);
            if (sub != null) return sub;

            var folder = new DirectoryViewFolder
            {
                Name = name,
                Image = icon,
                Path = string.Join("\\", hierarchy),
                Parents = hierarchy
            };
            Subfolders.Add(folder);
            return folder;
        }
    }

    public class FileViewFile
    {
        public Bitmap? Image { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public uint Size { get; set; }
        public string? SpecMask { get; set; }
        public ArchiveRecord File { get; set; } = null!;
    }

    public enum SpecMaskView
    {
        Flags,
        Bits,
        Hex
    }

    public enum DoubleClickAction
    {
        OpenFile,
        PreviewFile,
        Export
    }
}
