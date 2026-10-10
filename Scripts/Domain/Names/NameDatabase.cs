using System.Collections.Generic;
using AdventurersGuild.Core.Localization;

namespace AdventurersGuild.Domain.Names;

public class NameDatabase
{
    public List<LocalizedName> FirstNamesMale { get; set; }
    public List<LocalizedName> FirstNamesFemale { get; set; }
    public List<LocalizedName> Surnames { get; set; }
}