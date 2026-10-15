using System.Collections.Generic;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Adventurers;
using Godot;

namespace AdventurersGuild.Presentation.Adventurers;

public partial class AvailableBoard : PanelContainer
{
    private AdventurerList adventurerList;

    public AdventurerList AdventurerList => adventurerList;

    public override void _Ready()
    {
        if (!this.TryGetInstance(out adventurerList))
        {
            GD.PushError($"{nameof(AvailableBoard)}: AdventurerList не найден");
            return;
        }

        adventurerList.Clear();
    }

    public void Add(UnitModel model) => adventurerList?.Add(model);
    public void Remove(string id) => adventurerList?.Remove(id);
    public void Refresh(IReadOnlyList<UnitModel> available) => adventurerList?.Refresh(available);
}