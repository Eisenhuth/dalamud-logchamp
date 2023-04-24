using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using ImGuiNET;

namespace logchamp;

public class LogchampPlugin : IDalamudPlugin
    {
        public string Name =>"LogChamp";
        private const string commandName = "/logs";
        private static bool drawConfiguration;
        
        private Configuration configuration;
        private ChatGui chatGui;
        private Configuration.Timeframe configTimeframe;
        [PluginService] private static DalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] private static CommandManager CommandManager { get; set; } = null!;
        
        
        public LogchampPlugin([RequiredVersion("1.0")] DalamudPluginInterface dalamudPluginInterface, [RequiredVersion("1.0")] ChatGui chatGui, [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.chatGui = chatGui;

            configuration = (Configuration) dalamudPluginInterface.GetPluginConfig() ?? new Configuration();
            configuration.Initialize(dalamudPluginInterface);

            LoadConfiguration();
            
            dalamudPluginInterface.UiBuilder.Draw += DrawConfiguration;
            dalamudPluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
            
            commandManager.AddHandler(commandName, new CommandInfo(CleanupCommand)
            {
                HelpMessage = "opens the configuration",
                ShowInHelp = true
            });

            Task.Run(() => DeleteLogs(configTimeframe));
        }
        private void CleanupCommand(string command, string args)
        {
            OpenConfig();
        }

        private async Task DeleteLogs(Configuration.Timeframe timeframe)
        {
            var directoryInfo = new DirectoryInfo(configuration.LogsDirectory);
            var initialSize = await Task.Run(() => directoryInfo.GetTotalSize("*.log"));

            var filesToDelete = directoryInfo.GetFilesOlderThan(timeframe).ToList();

            if (filesToDelete.Count == 0)
                return;

            foreach (var file in filesToDelete)
            {
                try
                {
                    if(file.Exists && !file.IsReadOnly)
                        file.Delete();
                }
                catch (Exception exception)
                {
                    chatGui.Print($"{Name}: error deleting {file.Name}\n{exception}");
                }
            }

            directoryInfo = new DirectoryInfo(configuration.LogsDirectory);
            var newSize = await Task.Run(() => directoryInfo.GetTotalSize("*.log"));
            
            chatGui.Print($"{Name}: deleted {filesToDelete.Count} log(s) older than {timeframe.ToName()} with a total size of {(initialSize-newSize).FormatFileSize()}");
        }

        #region configuration
        
        private void DrawConfiguration()
        {
            if (!drawConfiguration)
                return;
            
            ImGui.Begin($"{Name} Configuration", ref drawConfiguration);
            
            ImGui.Text("Delete logs after");
            ImGui.SameLine();

            Enum timeframeRef = configTimeframe;
            if (DrawEnumCombo(ref timeframeRef))
            {
                configTimeframe = (Configuration.Timeframe) timeframeRef;
                SaveConfiguration();
            }
            
            var directoryInfo = new DirectoryInfo(configuration.LogsDirectory);
            var seven = directoryInfo.GetFilesOlderThan(7).ToList();
            var thirty = directoryInfo.GetFilesOlderThan(30).ToList();
            var ninety = directoryInfo.GetFilesOlderThan(90).ToList();
            
            ImGui.TextDisabled($"Logs older than 7 days: {seven.Count} files - {seven.Sum(file => file.Length).FormatFileSize()}");
            ImGui.TextDisabled($"Logs older than 30 days: {thirty.Count} files - {thirty.Sum(file => file.Length).FormatFileSize()}");
            ImGui.TextDisabled($"Logs older than 90 days: {ninety.Count} files - {ninety.Sum(file => file.Length).FormatFileSize()}");
            
            ImGui.End();
        }
        
        private static bool DrawEnumCombo(ref Enum value)
        {
            var valueChanged = false;
        
            if (ImGui.BeginCombo($"##EnumCombo{value.GetType()}", value.ToName()))
            {
                foreach (Enum enumValue in Enum.GetValues(value.GetType()))
                {
                    if (ImGui.Selectable(enumValue.ToName(), enumValue.Equals(value)))
                    {
                        value = enumValue;
                        valueChanged = true;
                    }
                }
            
                ImGui.EndCombo();
            }

            return valueChanged;
        }
        
        private static void OpenConfig()
        {
            drawConfiguration = true;
        }

        private void LoadConfiguration()
        {
            configTimeframe = configuration.DeleteAfterTimeframe;
        }

        private void SaveConfiguration()
        {
            configuration.DeleteAfterTimeframe = configTimeframe;
            Task.Run(() => DeleteLogs(configTimeframe));

            PluginInterface.SavePluginConfig(configuration);
        }
        #endregion
        
        public void Dispose()
        {
            PluginInterface.UiBuilder.Draw -= DrawConfiguration;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;

            CommandManager.RemoveHandler(commandName);
        }
    }