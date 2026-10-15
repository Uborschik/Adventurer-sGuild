using AdventurersGuild.Domain.Adventurers;

namespace AdventurersGuild.Presentation.Common;

public interface IAdventurerRow
{
    void Bind(UnitModel model);
}