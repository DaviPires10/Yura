using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Yura
{
    public static class FilePicker
    {
        public static async Task<string?> OpenFile(Window owner, string title,
            params (string name, string[] patterns)[] filters)
        {
            var top = TopLevel.GetTopLevel(owner);
            if (top is null) return null;

            var opts = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = filters.Select(f => new FilePickerFileType(f.name) { Patterns = f.patterns }).ToList()
            };
            var result = await top.StorageProvider.OpenFilePickerAsync(opts);
            return result.FirstOrDefault()?.TryGetLocalPath();
        }

        public static async Task<string?> SaveFile(Window owner, string title,
            string suggestedName, params (string name, string[] patterns)[] filters)
        {
            var top = TopLevel.GetTopLevel(owner);
            if (top is null) return null;

            var opts = new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = suggestedName,
                FileTypeChoices = filters.Select(f => new FilePickerFileType(f.name) { Patterns = f.patterns }).ToList()
            };
            var file = await top.StorageProvider.SaveFilePickerAsync(opts);
            return file?.TryGetLocalPath();
        }

        public static async Task<string?> OpenFolder(Window owner, string title)
        {
            var top = TopLevel.GetTopLevel(owner);
            if (top is null) return null;

            var result = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            });
            return result.FirstOrDefault()?.TryGetLocalPath();
        }
    }
}
