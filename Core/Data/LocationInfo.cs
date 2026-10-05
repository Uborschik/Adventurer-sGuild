using System.Collections.Generic;

public class LocationInfo
{
    public string Id { get; set; }
    public LocalizedNoun Name { get; set; }
}

public class LocationDatabase
{
    public List<LocationInfo> Locations { get; set; } = new();
}
