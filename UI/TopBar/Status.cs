using Godot;
using System;

public partial class Status : Control
{
    private Label gold;
    private Label glory;
    private Label day;
    private Label time;

    private GuildBank bank;
    private GameClock clock;

    public override void _Ready()
    {
        gold = GetNode<Label>("Gold/Count");
        glory = GetNode<Label>("Glory/Count");
        day = GetNode<Label>("Day/Count");
        time = GetNode<Label>("Time/Count");
    }

    public override void _ExitTree()
    {
        if (bank != null)
        {
            bank.GoldChanged -= RefreshGold;
            bank.GloryChanged -= RefreshGlory;
        }
        if (clock != null)
            clock.TimeAdvanced -= OnTimeAdvanced;
    }

    public void Bind(GuildBank bank, GameClock clock)
    {
        this.bank = bank;
        this.clock = clock;

        bank.GoldChanged += RefreshGold;
        bank.GloryChanged += RefreshGlory;
        clock.TimeAdvanced += OnTimeAdvanced;

        RefreshGold();
        RefreshGlory();
        RefreshTime();
    }

    private void RefreshGold() => gold.Text = bank.Gold.ToString();
    private void RefreshGlory() => glory.Text = bank.Glory.ToString();
    private void OnTimeAdvanced(GameTime t) => RefreshTime();
    private void RefreshTime()
    {
        var t = clock.Now;
        day.Text = t.Day.ToString();
        time.Text = $"{t.Hour:D2}:{t.Minute:D2}";
    }
}
