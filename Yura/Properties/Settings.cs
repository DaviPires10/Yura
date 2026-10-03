using System;
using System.IO;
using System.Text.Json;

namespace Yura
{
    public class Settings
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Yura", "settings.json");

        public static Settings Default { get; private set; } = Load();

        public int Theme { get; set; }
        public int SpecMaskView { get; set; }
        public int ClickAction { get; set; }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }

        private static Settings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
            }
            catch { }
            return new Settings();
        }
    }
}
