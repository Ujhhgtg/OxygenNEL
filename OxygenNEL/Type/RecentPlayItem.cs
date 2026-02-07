/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/

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
