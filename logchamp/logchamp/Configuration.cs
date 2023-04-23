using System;
using System.ComponentModel;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace logchamp;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; }
    public string LogsDirectory { get; set; } = Environment.ExpandEnvironmentVariables("%AppData%\\Advanced Combat Tracker\\FFXIVLogs");
    
    public enum Timeframe
    {
     [Description("1 week")] Seven,
     [Description("2 weeks")] Fourteen,
     [Description("1 month")] Thirty,
     [Description("2 months")] Sixty,
     [Description("3 months")] Ninety
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