using System.Collections.Generic;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Units;
using Godot;

namespace AdventurersGuild.Presentation.Adventurers;

public partial class AdventurerBoard : Control
{
    private AdventurerList adventurerList;
    private UnitRegistry adventurerRegistry;

    public AdventurerList AdventurerList => adventurerList;

    public override void _Ready()
    {
        if (!this.TryGetInstance(out adventurerList))
        {
            GD.PushError($"{nameof(AdventurerBoard)}: AdventurerList не найден");
            return;
        }

        adventurerList.Clear();
    }

    public override void _ExitTree()
    {
        if (adventurerRegistry != null)
        {
            adventurerRegistry.Added -= Add;
            adventurerRegistry.Removed -= Remove;
        }
    }

    public void Bind(UnitRegistry adventurerRegistry)
    {
        this.adventurerRegistry = adventurerRegistry;

        adventurerRegistry.Added += Add;
        adventurerRegistry.Removed += Remove;
    }

    public void Add(UnitModel model) => adventurerList.Add(model);
    public void Remove(string id) => adventurerList?.Remove(id);
    public void Refresh() => adventurerList.Refresh(adventurerRegistry.All);
    public void Refresh(IReadOnlyList<UnitModel> hired) => adventurerList.Refresh(hired);
}