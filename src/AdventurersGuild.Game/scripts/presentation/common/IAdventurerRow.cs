using AdventurersGuild.Domain.Units;

namespace AdventurersGuild.Presentation.Common;

public interface IAdventurerRow
{
    void Bind(UnitModel model);
}