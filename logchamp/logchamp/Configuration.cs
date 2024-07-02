using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace logchamp;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; }
    public string LogsDirectory { get; set; } = Environment.ExpandEnvironmentVariables("%AppData%\\Advanced Combat Tracker\\FFXIVLogs");
    public string DeucalionDirectory { get; set; } = Environment.ExpandEnvironmentVariables("%AppData%\\deucalion");
    public long TotalDeleted { get; set; } = 0;

    public enum Timeframe
    {
        Seven,
        Fourteen,
        Thirty,
        Sixty,
        Ninety
    }
    
    public Timeframe DeleteAfterTimeframe = Timeframe.Thirty;

    private IDalamudPluginInterface _pluginInterface;

    public void Initialize(IDalamudPluginInterface pInterface)
    {
        _pluginInterface = pInterface;
    }

    public void Save()
    {
        _pluginInterface.SavePluginConfig(this);
    }
}