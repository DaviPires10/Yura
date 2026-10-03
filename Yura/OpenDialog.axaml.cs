using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using Yura.Shared.IO;
using Yura.Shared.Util;

namespace Yura
{
    public partial class OpenDialog : Window, IFileSettings
    {
        public OpenDialog()
        {
            AvaloniaXamlLoader.Load(this);

            var folder = Path.Combine(AppContext.BaseDirectory, "FileLists");
            var lists = new List<FileListItem>();

            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.GetFiles(folder, "*.txt"))
                {
                    lists.Add(new FileListItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Path = file
                    });
                }
            }

            this.FindControl<ComboBox>("FileListSelect")!.ItemsSource = lists;
        }

        private void OkButton_Click(object? sender, RoutedEventArgs e) => Close(true);

        public Endianness Endianness =>
            (Endianness)this.FindControl<ComboBox>("EndiannessSelect")!.SelectedIndex;

        public int Alignment =>
            Convert.ToInt32(this.FindControl<TextBox>("AlignmentField")!.Text, 16);

        public Game Game
        {
            get => (Game)this.FindControl<ComboBox>("GameSelect")!.SelectedIndex;
            set
            {
                this.FindControl<ComboBox>("GameSelect")!.SelectedIndex = (int)value;
                this.FindControl<TextBox>("AlignmentField")!.IsEnabled = value < Game.DeusEx;
            }
        }

        public Platform Platform =>
            (Platform)this.FindControl<ComboBox>("PlatformSelect")!.SelectedIndex;

        public string FileList =>
            (this.FindControl<ComboBox>("FileListSelect")!.SelectedItem as FileListItem)?.Path!;

        public class FileListItem
        {
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
        }

        private void GameSelect_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var cb = this.FindControl<ComboBox>("GameSelect");
            var txt = this.FindControl<TextBox>("AlignmentField");
            if (cb != null && txt != null)
                txt.IsEnabled = cb.SelectedIndex <= (int)Game.Legend;
        }
    }

    public enum Game { Defiance, Legend, DeusEx, Tiger }
}
