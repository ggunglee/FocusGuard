using System;
using System.IO;
using System.Text.Json;

namespace FocusGuard
{
    public static class AppSettings
    {
        private static readonly string SettingsDir =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FocusGuard"
            );

        public static readonly string SettingsPath =
            Path.Combine(SettingsDir, "focusguard.settings.json");

        private static readonly string LegacySettingsPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "focusguard.settings.json");

        public static double FocusedOpacity { get; set; } = 0.05;
        public static bool IsRealRestMode { get; set; } = false;
        public static string ReadingLogUrl { get; set; } = "";

        public static void Load()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);

                // 예전 버전이 EXE 폴더에 저장했던 설정이 있으면 새 위치로 복사
                if (!File.Exists(SettingsPath) && File.Exists(LegacySettingsPath))
                {
                    File.Copy(LegacySettingsPath, SettingsPath, overwrite: false);
                }

                if (!File.Exists(SettingsPath)) return;

                string json = File.ReadAllText(SettingsPath);
                var data = JsonSerializer.Deserialize<AppSettingsData>(json);
                if (data == null) return;

                FocusedOpacity = Math.Clamp(data.FocusedOpacity, 0.05, 1.0);
                IsRealRestMode = data.IsRealRestMode;
                ReadingLogUrl = data.ReadingLogUrl ?? "";
            }
            catch
            {
                FocusedOpacity = 0.05;
                IsRealRestMode = false;
                ReadingLogUrl = "";
            }
        }

        public static void Save()
        {
            Directory.CreateDirectory(SettingsDir);

            var data = new AppSettingsData
            {
                FocusedOpacity = FocusedOpacity,
                IsRealRestMode = IsRealRestMode,
                ReadingLogUrl = ReadingLogUrl ?? ""
            };

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SettingsPath, json);
        }

        private class AppSettingsData
        {
            public double FocusedOpacity { get; set; } = 0.05;
            public bool IsRealRestMode { get; set; } = false;
            public string ReadingLogUrl { get; set; } = "";
        }
    }
}
