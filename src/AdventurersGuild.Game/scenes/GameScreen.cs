using Godot;
using System.Text;

public partial class GameScreen : Control
{
    private RichTextLabel mapText;
    private RichTextLabel logText;
    private RichTextLabel statusText;
    private Label mapTooltip;

    private enum GameState { Explore, Busy }
    private GameState currentState = GameState.Explore;

    private Area currentNode;

    private double blinkTimer = 0;
    private bool showAtSymbol = true;

    private readonly Area[,] mapGrid = new Area[5, 5];

    public override void _Ready()
    {
        mapText = GetNode<RichTextLabel>("MainVBox/LogPanel/HBox/VBoxMap/MapPanel/MapText");
        mapTooltip = GetNode<Label>("MainVBox/LogPanel/HBox/VBoxMap/TooltipContainer/MapTooltip");
        logText = GetNode<RichTextLabel>("MainVBox/LogPanel/HBox/LogContainer/LogText");
        statusText = GetNode<RichTextLabel>("MainVBox/BottomPanel/StatusText");

        mapText.MetaClicked += OnMapClicked;
        logText.MetaClicked += OnLogClicked;

        mapText.MetaHoverStarted += OnMapHoverStarted;
        mapText.MetaHoverEnded += OnMapHoverEnded;

        BuildMap();
        UpdateUI();
    }

    public override void _Process(double delta)
    {
        if (currentState == GameState.Explore && currentNode != null)
        {
            blinkTimer += delta;
            
            if (blinkTimer >= 0.5)
            {
                blinkTimer -= 0.5;
                showAtSymbol = !showAtSymbol;
                UpdateMap();
            }
        }
    }

    private void BuildMap()
    {
        var guild = new Area { Id = "guild", Name = "Гильдия", Symbol = 'G', Color = "yellow", Description = "Безопасная зона.", Connections = { "forest", "road", "wolves" } };
        var forest = new Area { Id = "forest", Name = "Лес", Symbol = 'F', Color = "green", Description = "Древний лес. Водятся волки.", Connections = { "guild" } };
        var road = new Area { Id = "road", Name = "Тракт", Symbol = 'R', Color = "gray", Description = "Торговый путь.", Connections = { "guild" } };
        var wolves = new Area { Id = "wolves", Name = "Волки", Symbol = 'E', Color = "red", Description = "Стая волков. Опасность!", Connections = { "guild" } };

        var roadH = new Area { Symbol = '-', Color = "gray" };
        var roadV = new Area { Symbol = '|', Color = "gray" };

        mapGrid[0, 2] = wolves;
        mapGrid[1, 2] = roadV;
        mapGrid[2, 0] = forest;
        mapGrid[2, 1] = roadH;
        mapGrid[2, 2] = guild;
        mapGrid[2, 3] = roadH;
        mapGrid[2, 4] = road;

        currentNode = guild;
        currentNode.IsVisited = true;
    }

    private void UpdateMap()
    {
        var sb = new StringBuilder();

        int rows = mapGrid.GetLength(0);
        int cols = mapGrid.GetLength(1);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                var area = mapGrid[y, x];

                if (area == null)
                {
                    sb.Append(' ');
                }
                else
                {
                    sb.Append(GetFormattedSymbol(area));
                }
            }
            sb.Append('\n');
        }

        mapText.Text = sb.ToString();
    }

    private string GetFormattedSymbol(Area area)
    {
        string visual;
        
        if (area.IsRoad)
        {
            visual = $"{area.Symbol}"; 
        }
        else if (area == currentNode)
        {
            char activeSymbol = showAtSymbol ? '@' : area.Symbol;
            visual = $"[{activeSymbol}]"; 
        }
        else
        {
            visual = $" {area.Symbol} "; 
        }

        if (area.IsRoad)
            return $"[color={area.Color}]{visual}[/color]";

        if (area == currentNode)
            return $"[color=yellow][url={area.Id}]{visual}[/url][/color]";

        if (IsAccessible(area) && currentState == GameState.Explore)
            return $"[color=cyan][url={area.Id}]{visual}[/url][/color]";

        string color = area.IsVisited ? "gray" : area.Color;
        return $"[color={color}]{visual}[/color]";
    }

    private bool IsAccessible(Area targetArea)
    {
        if (targetArea == null || targetArea.IsRoad) return false;
        return currentNode.Connections.Contains(targetArea.Id);
    }

    private Area FindAreaById(string id)
    {
        foreach (var area in mapGrid)
        {
            if (area != null && area.Id == id) return area;
        }
        return null;
    }

    private void MoveToNode(string nodeId)
    {
        if (currentState != GameState.Explore) return;

        var targetArea = FindAreaById(nodeId);
        if (targetArea == null || !IsAccessible(targetArea)) return;

        currentNode = targetArea;
        currentNode.IsVisited = true;
        UpdateUI();
    }

    private void OnMapHoverStarted(Variant meta)
    {
        string id = meta.AsString();
        var area = FindAreaById(id);
        
        if (area != null)
        {
            string suffix = area == currentNode ? " (Вы здесь)" : "";
            mapTooltip.Text = $"{area.Name}{suffix}: {area.Description}";
        }
    }

    private void OnMapHoverEnded(Variant meta) => mapTooltip.Text = "";

    private void OnMapMouseExited() => mapTooltip.Text = "";


    private void OnMapClicked(Variant meta) => MoveToNode(meta.AsString());

    private void OnLogClicked(Variant meta)
    {
        string action = meta.AsString();
        if (action == "fight") StartCombat();
        else if (action == "continue") { currentState = GameState.Explore; UpdateUI(); }
        else MoveToNode(action);
    }

    private void StartCombat()
    {
        currentState = GameState.Busy;
        UpdateMap(); 
        logText.Text = "[color=red]--- Бой ---[/color]\nВолки атакуют!\n[color=green]Победа![/color]\n[color=cyan][url=continue]Продолжить[/url][/color]";
    }

    // --- UI И ТЕКСТ ---
    private void UpdateUI()
    {
        UpdateMap();
        UpdateStatus();
        if (currentState == GameState.Explore) UpdateLogExploration();
    }

    private void UpdateLogExploration()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Вы находитесь в локации: [b]{currentNode.Name}[/b].\n");

        if (currentNode.Id == "forest")
        {
            sb.AppendLine("Деревья вокруг шелестят. Вы слышите волчий вой.");
            sb.AppendLine("[color=cyan][url=fight]Приготовиться к бою[/url][/color]\n");
        }

        sb.AppendLine("[color=gold]Доступные пути:[/color]");
        foreach (var connId in currentNode.Connections)
        {
            var target = FindAreaById(connId);
            if (target != null)
            {
                sb.AppendLine($"[color=cyan][url={target.Id}]Пойти в {target.Name} ({target.Symbol})[/url][/color]");
            }
        }

        logText.Text = sb.ToString();
    }

    private void UpdateStatus() => statusText.Text = "[ Гаррет: 24/24 HP ] | Золото: 50 | День: 1";
}