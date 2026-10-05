using Godot;

public partial class AdventurerBoard : Control
{
    private AdventurerList adventurerList;
    private Context context;
    private AdventurerInfo adventurerInfo;

    private AdventurerRegistry adventurerRegistry;

    public AdventurerList AdventurerList => adventurerList;
    public Context Context => context;
    public AdventurerInfo AdventurerInfo => adventurerInfo;

    public override void _Ready()
    {
        if (!this.TryGetInstance(out adventurerList)) return;
        if (!this.TryGetInstance(out context)) return;
        if (!this.TryGetInstance(out adventurerInfo)) return;

        adventurerList.SelectedIdChanged += OnSelectedIdChanged;

        adventurerList.Clear();
    }

    public override void _ExitTree()
    {
        if (adventurerList != null)
            adventurerList.SelectedIdChanged -= OnSelectedIdChanged;

        if (adventurerRegistry != null)
            adventurerRegistry.Removed -= OnRemove;
    }

    public void Bind(AdventurerRegistry adventurerRegistry)
    {
        this.adventurerRegistry = adventurerRegistry;
        adventurerRegistry.Removed += OnRemove;
    }

    private void OnRemove(AdventurerModel model) => Remove(model.Id);

    public void OnSelectedIdChanged(string id)
    {
        var model = adventurerRegistry.GetById(id);

        adventurerInfo.SetAdventurer(model);
    }

    public bool TryAdd(AdventurerModel model)
    {
        if (!adventurerRegistry.TryAdd(model)) return false;

        adventurerList.Add(model);

        return true;
    }

    public void Remove(string id)
    {
        adventurerList.Remove(id);
        adventurerInfo.Clear();
    }
}
