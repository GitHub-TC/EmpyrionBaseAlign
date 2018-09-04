using EmpyrionAPIDefinitions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace EmpyrionBaseAlign
{
    public class Configuration
    {
        public PermissionType FreePermissionLevel { get; set; } = PermissionType.Moderator;
        public string[] ForbiddenPlayfields { get; set; } = new string[] { "" };
    }

    public class ConfigurationManager
    {
        public FileSystemWatcher ConfigFileChangedWatcher { get; private set; }
        public string ConfigFilename { get; private set; }

        public Configuration CurrentConfiguration { get; set; }

        public static Action<string, LogLevel> Logger { get; set; }

        private static void log(string aText, LogLevel aLevel)
        {
            Logger?.Invoke(aText, aLevel);
        }

        public ConfigurationManager()
        {
            InitializeConfiguration();
            InitializeConfigurationFileWatcher();
        }

        private void InitializeConfigurationFileWatcher()
        {
            ConfigFileChangedWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(ConfigFilename),
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = Path.GetFileName(ConfigFilename)
            };
            ConfigFileChangedWatcher.Changed += (s, e) => ReadConfiguration();
            ConfigFileChangedWatcher.EnableRaisingEvents = true;
        }

        private void InitializeConfiguration()
        {
            ConfigFilename = Path.Combine(EmpyrionConfiguration.ProgramPath, @"Saves\Games\" + EmpyrionConfiguration.DedicatedYaml.SaveGameName + @"\Mods\EmpyrionBaseAlign\Config.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilename));

            ReadConfiguration();
        }

        public void SaveConfiguration()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(Configuration));
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilename));
                using (var writer = XmlWriter.Create(ConfigFilename, new XmlWriterSettings() { Indent = true, IndentChars = "  " }))
                {
                    serializer.Serialize(writer, CurrentConfiguration);
                }
            }
            catch (Exception Error)
            {
                log("Configuration " + Error.ToString(), LogLevel.Error);
            }
        }

        public void ReadConfiguration()
        {
            if (!File.Exists(ConfigFilename))
            {
                log($"Configuration not found '{ConfigFilename}'", LogLevel.Error);
                CurrentConfiguration = new Configuration();
                SaveConfiguration();
            }

            try
            {
                log($"Configuration load '{ConfigFilename}'", LogLevel.Message);
                var serializer = new XmlSerializer(typeof(Configuration));
                using (var reader = XmlReader.Create(ConfigFilename))
                {
                    CurrentConfiguration = (Configuration)serializer.Deserialize(reader);
                }
            }
            catch (Exception Error)
            {
                log("Configuration " + Error.ToString(), LogLevel.Error);
                CurrentConfiguration = new Configuration();
                SaveConfiguration();
            }
        }

    }
}
