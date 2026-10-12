using System;

namespace AdventurersGuild.Domain.Common;

public class GuildBank(int startGold = 0, int startGlory = 0)
{
    public int Gold { get; private set; } = startGold;
    public int Glory { get; private set; } = startGlory;

    public event Action GoldChanged;
    public event Action GloryChanged;

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        GoldChanged?.Invoke();
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return false;
        if (Gold < amount) return false;

        Gold -= amount;
        GoldChanged?.Invoke();
        return true;
    }

    public void AddGlory(int amount)
    {
        if (amount == 0) return;
        Glory += amount;
        GloryChanged?.Invoke();
    }

    public bool TrySpendGlory(int amount)
    {
        if (amount <= 0) return false;
        if (Glory < amount) return false;

        Glory -= amount;
        GloryChanged?.Invoke();
        return true;
    }
}