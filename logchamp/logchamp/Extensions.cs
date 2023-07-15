using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace logchamp;

public static class Extensions
{
    public static string FormatFileSize(this long bytes)
    {
        const int scale = 1024;
        string[] units = {"B", "KB", "MB", "GB"};

        var value = (double) bytes;
        var magnitude = 0;

        while (value >= scale && magnitude < units.Length - 1)
        {
            value /= scale;
            magnitude++;
        }

        return $"{value:0.##} {units[magnitude]}";
    }
    
    public static IEnumerable<FileInfo> GetFilesOlderThan(this DirectoryInfo directory, int days)
    {
        var cutoff = DateTime.Now.AddDays(-days);

        return directory.GetFiles().Where(file => file.LastWriteTime < cutoff);
    }
    
    public static IEnumerable<FileInfo> GetFilesOlderThan(this DirectoryInfo directory, Configuration.Timeframe timeframe)
    {
        var days = timeframe switch
        {
            Configuration.Timeframe.Seven => 7,
            Configuration.Timeframe.Fourteen => 14,
            Configuration.Timeframe.Thirty => 30,
            Configuration.Timeframe.Sixty => 60,
            Configuration.Timeframe.Ninety => 90,
            _ => throw new ArgumentOutOfRangeException(nameof(timeframe), timeframe, null)
        };

        var cutoff = DateTime.Now.AddDays(-days);

        return directory.GetFiles().Where(file => file.LastWriteTime < cutoff);
    }

    public static long GetTotalSize(this DirectoryInfo directory, string searchPattern)
    {
        return directory.Exists ? directory.EnumerateFiles(searchPattern, SearchOption.AllDirectories).Sum(file => file.Length) : 0;
    }

    public static string ToName(this Enum timeframe)
    {
        return timeframe switch
        {
            Configuration.Timeframe.Seven => "1 week",
            Configuration.Timeframe.Fourteen => "2 weeks",
            Configuration.Timeframe.Thirty => "1 month",
            Configuration.Timeframe.Sixty => "2 months",
            Configuration.Timeframe.Ninety => "3 months",
            _ => string.Empty,
        };
    }
}