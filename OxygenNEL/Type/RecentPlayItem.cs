using System;

namespace OxygenNEL.type;

public class RecentPlayItem
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string ServerType { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public DateTime LastPlayTime { get; set; }
}
