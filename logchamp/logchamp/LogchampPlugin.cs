using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ImGuiNET;

namespace logchamp;

public class LogchampPlugin : IDalamudPlugin
    {
        public string Name =>"LogChamp";
        private const string commandName = "/logs";
        private static bool drawConfiguration;
        
        private Configuration configuration;
        private IChatGui chatGui;
        private Configuration.Timeframe configTimeframe;
        private string configLogsDirectory;
        [PluginService] private static DalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] private static ICommandManager CommandManager { get; set; } = null!;

        private bool cleanedOnStartup;
        
        
        public LogchampPlugin([RequiredVersion("1.0")] DalamudPluginInterface dalamudPluginInterface, [RequiredVersion("1.0")] IChatGui chatGui, [RequiredVersion("1.0")] ICommandManager commandManager)
        {
            this.chatGui = chatGui;

            configuration = (Configuration) dalamudPluginInterface.GetPluginConfig() ?? new Configuration();
            configuration.Initialize(dalamudPluginInterface);

            LoadConfiguration();
            
            dalamudPluginInterface.UiBuilder.Draw += DrawConfiguration;
            dalamudPluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
            
            chatGui.ChatMessage += OnChatMessage;
            
            commandManager.AddHandler(commandName, new CommandInfo(CleanupCommand)
            {
                HelpMessage = "opens the configuration",
                ShowInHelp = true
            });
        }
        private void OnChatMessage(XivChatType type, uint senderid, ref SeString sender, ref SeString message, ref bool ishandled)
        {
            if (type == XivChatType.Notice && !cleanedOnStartup)
            {
                Task.Run(() => DeleteLogs(configTimeframe));
                cleanedOnStartup = true;
            }
        }

        private void CleanupCommand(string command, string args)
        {
            OpenConfig();
        }

        private async Task DeleteLogs(Configuration.Timeframe timeframe)
        {
            var logsDirectoryInfo = new DirectoryInfo(configuration.LogsDirectory);
            
            if(!logsDirectoryInfo.Exists)
            {
                chatGui.Print($"{Name}: couldn't find directory, please check the configuration -> /logs");
                return;
            }
            
            var deucalionDirectoryInfo = new DirectoryInfo(configuration.DeucalionDirectory);
            var initialSize = await Task.Run(() => logsDirectoryInfo.GetTotalSize("*.log") + deucalionDirectoryInfo.GetTotalSize("*.log"));
            var filesToDelete = logsDirectoryInfo.GetFilesOlderThan(timeframe).ToList();
            filesToDelete.AddRange(deucalionDirectoryInfo.GetFilesOlderThan(timeframe).ToList());
            
            if (filesToDelete.Count == 0)
                return;

            foreach (var file in filesToDelete)
            {
                try
                {
                    if(file.Exists && file.Extension.Equals(".log") && !file.IsReadOnly)
                        file.Delete();
                }
                catch (Exception exception)
                {
                    chatGui.Print($"{Name}: error deleting {file.Name}\n{exception}");
                }
            }

            logsDirectoryInfo = new DirectoryInfo(configuration.LogsDirectory);
            deucalionDirectoryInfo = new DirectoryInfo(configuration.DeucalionDirectory);
            var newSize = await Task.Run(() => logsDirectoryInfo.GetTotalSize("*.log") + deucalionDirectoryInfo.GetTotalSize("*.log"));
            
            chatGui.Print($"{Name}: deleted {filesToDelete.Count} log(s) older than {timeframe.ToName()} with a total size of {(initialSize-newSize).FormatFileSize()}");
        }

        #region configuration
        
        private void DrawConfiguration()
        {
            if (!drawConfiguration)
                return;
            
            ImGui.Begin($"{Name} Configuration", ref drawConfiguration);

            if (ImGui.InputText("Logs Directory", ref configLogsDirectory, 256))
                SaveConfiguration();
            
            ImGui.Text("Delete logs after");
            ImGui.SameLine();

            Enum timeframeRef = configTimeframe;
            if (DrawEnumCombo(ref timeframeRef))
            {
                configTimeframe = (Configuration.Timeframe) timeframeRef;
                SaveConfiguration();
            }
            
            var directoryInfo = new DirectoryInfo(configuration.LogsDirectory);
            if (directoryInfo.Exists)
            {
                var seven = directoryInfo.GetFilesOlderThan(7).ToList();
                var thirty = directoryInfo.GetFilesOlderThan(30).ToList();
                var ninety = directoryInfo.GetFilesOlderThan(90).ToList();
            
                ImGui.TextDisabled($"Logs older than 7 days: {seven.Count} files - {seven.Sum(file => file.Length).FormatFileSize()}");
                ImGui.TextDisabled($"Logs older than 30 days: {thirty.Count} files - {thirty.Sum(file => file.Length).FormatFileSize()}");
                ImGui.TextDisabled($"Logs older than 90 days: {ninety.Count} files - {ninety.Sum(file => file.Length).FormatFileSize()}");
            }
            else
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Logs directory doesn't exist");

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
            configLogsDirectory = configuration.LogsDirectory;
        }

        private void SaveConfiguration()
        {
            configuration.DeleteAfterTimeframe = configTimeframe;
            configuration.LogsDirectory = configLogsDirectory;
            Task.Run(() => DeleteLogs(configTimeframe));

            PluginInterface.SavePluginConfig(configuration);
        }
        #endregion
        
        public void Dispose()
        {
            PluginInterface.UiBuilder.Draw -= DrawConfiguration;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
            chatGui.ChatMessage -= OnChatMessage;

            CommandManager.RemoveHandler(commandName);
        }
    }