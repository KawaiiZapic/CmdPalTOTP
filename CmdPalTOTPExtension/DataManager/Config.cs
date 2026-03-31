using CmdPalTOTPExtension.DataModel;
using Microsoft.Windows.Storage;
using System;
using System.IO;
using System.Text.Json;

namespace CmdPalTOTPExtension.DataManager {

    public class Config {

        private readonly string fileName = Path.Combine(ApplicationData.GetDefault().LocalPath, "OTPList.json");
        private readonly int CurrentVersion = 3;
        private readonly JsonSerializerOptions jsonSerializerOptions = new() {
            IndentSize = 4
        };
        public AuthenticatorsList Authenticators { get; private set; }

        public static Config Instance { get; } = new Config();

        private Config() {
            Load();
            if (Authenticators is null) {
                throw new Exception("Failed to load config");
            }
        }

        public void Load(string path) {
            if (File.Exists(path)) {
                try {
                    Authenticators = JsonSerializer.Deserialize<AuthenticatorsList>(File.ReadAllText(path));
                } catch { }
            }
            Authenticators ??= new AuthenticatorsList();
            Authenticators.Version = CurrentVersion;
        }

        public void Load() => Load(fileName);

        public void Save() {
            File.WriteAllText(fileName, JsonSerializer.Serialize<AuthenticatorsList>(Authenticators, jsonSerializerOptions));
        }
    }
}
