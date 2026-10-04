using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class DebugResolvedTable : Control
{
    private GridContainer tagsGrid;
    private GridContainer templatesGrid;
    private GridContainer activeGrid;

    private QuestRegistry questRegistry;

    public override void _Ready()
    {
        tagsGrid = GetNode<GridContainer>("Margin/VBox/TagsGrid");
        templatesGrid = GetNode<GridContainer>("Margin/VBox/TemplatesScroll/TemplatesGrid");
        activeGrid = GetNode<GridContainer>("Margin/VBox/ActiveScroll/ActiveGrid");
    }

    public void Bind(QuestRegistry registry)
    {
        questRegistry = registry;
    }

    public void Toggle()
    {
        Visible = !Visible;
        if (Visible) Rebuild();
    }

    private void Rebuild()
    {
        ClearChildren(tagsGrid);
        ClearChildren(templatesGrid);
        ClearChildren(activeGrid);

        BuildTags();
        BuildTemplates();
        BuildActiveQuests();
    }

    private void BuildTags()
    {
        AddRow(tagsGrid, "Tag", "Weights", "Tier");

        var tags = QuestDatabase.AllTags;
        if (tags == null) return;

        foreach (var tag in tags.Values.OrderBy(t => t.Id))
        {
            string weights = tag.Weights != null && tag.Weights.Count > 0
                ? string.Join(", ", tag.Weights
                    .Where(kv => kv.Value > 0)
                    .Select(kv => $"{kv.Key}={kv.Value:F2}"))
                : "";
            string tier = string.IsNullOrEmpty(tag.Tier) ? "" : tag.Tier;

            AddRow(tagsGrid, tag.Id, weights, tier);
        }
    }

    private void BuildTemplates()
    {
        AddRow(templatesGrid, "Template", "Creature", "Tier", "Need", "Weights");

        var resolved = QuestDatabase.AllResolved;
        if (resolved == null) return;

        var sorted = resolved
            .OrderBy(kv => kv.Key.templateId)
            .ThenBy(kv => kv.Key.creatureId);

        foreach (var kv in sorted)
        {
            var (templateId, creatureId) = kv.Key;
            var r = kv.Value;

            string weights = SerializeWeights(r.Weights);   // ← r.Weights

            AddRow(templatesGrid,
                templateId,
                creatureId,
                r.Tier.ToString(),
                r.RecommendedPartySize.ToString(),
                weights);
        }
    }

    private void BuildActiveQuests()
    {
        AddRow(activeGrid, "Name", "Tier", "Need", "Weights");

        if (questRegistry == null) return;

        var sorted = questRegistry.All
            .Where(q => q.Status == QuestStatus.Available)
            .OrderBy(q => q.Name);

        foreach (var quest in sorted)
        {
            string weights = SerializeWeights(quest.Weights);

            AddRow(activeGrid,
                quest.Name,
                quest.Tier.ToString(),
                quest.RecommendedPartySize.ToString(),
                weights);
        }
    }

    private static void AddRow(GridContainer grid, params string[] values)
    {
        foreach (var v in values)
        {
            var label = new Label { Text = v };
            grid.AddChild(label);
        }
    }

    private static void ClearChildren(Node node)
    {
        foreach (var child in node.GetChildren())
            child.QueueFree();
    }

    private static string SerializeWeights(IReadOnlyDictionary<string, double> weights)
    {
        if (weights == null || weights.Count == 0) return "";

        return string.Join(", ",
            weights.Where(kv => kv.Value > 0)
                   .OrderBy(kv => kv.Key)
                   .Select(kv => $"{kv.Key}={kv.Value:F2}"));
    }
}