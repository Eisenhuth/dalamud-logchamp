using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace logchamp;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; }
    public string LogsDirectory { get; set; } = Environment.ExpandEnvironmentVariables("%AppData%\\Advanced Combat Tracker\\FFXIVLogs");
    
    public enum Timeframe
    {
        Seven,
        Fourteen,
        Thirty,
        Sixty,
        Ninety
    }
    
    public Timeframe DeleteAfterTimeframe = Timeframe.Thirty;

    private DalamudPluginInterface _pluginInterface;

    public void Initialize(DalamudPluginInterface pInterface)
    {
        _pluginInterface = pInterface;
    }

    public void Save()
    {
        _pluginInterface.SavePluginConfig(this);
    }
}