using System.Collections.Generic;
using AdventurersGuild.Core;
using AdventurersGuild.Domain.Adventurers;
using Godot;

namespace AdventurersGuild.Presentation.Adventurers;

public partial class AdventurerBoard : Control
{
    private AdventurerList adventurerList;
    private AdventurerRegistry adventurerRegistry;

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
            adventurerRegistry.Removed += adventurerList.Remove;
    }

    public void Bind(AdventurerRegistry adventurerRegistry)
    {
        this.adventurerRegistry = adventurerRegistry;

        adventurerRegistry.Removed += adventurerList.Remove;
    }

    public void Add(AdventurerModel model)
    {
        if (adventurerRegistry.TryAdd(model))
        {
            adventurerList.Add(model);
        }
    }
    public void Remove(string id)
    {
        if (adventurerRegistry.TryRemove(id))
        {
            adventurerList?.Remove(id);
        }
    }
    public void Refresh() => adventurerList.Refresh(adventurerRegistry.All);
    public void Refresh(IReadOnlyList<AdventurerModel> hired) => adventurerList.Refresh(hired);
}