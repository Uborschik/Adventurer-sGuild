using System;
using System.Collections.Generic;
using AdventurersGuild.Core;

namespace AdventurersGuild.Domain.Adventurers;

public class AdventurerRegistry
{
    public event Action<string> Added;
    public event Action<string> Removed;

    private readonly List<AdventurerModel> data = [];

    public IReadOnlyList<AdventurerModel> All => data;
    public int Count => data.Count;

    public void Tick(GameTime now)
    {
        for (int i = data.Count - 1; i >= 0; i--)
        {
            var model = data[i];

            if (model.ShouldBeRemoved(now))
            {
                data.RemoveAt(i);
                Removed?.Invoke(model.Id);
                continue;
            }

            model.Tick(now);
        }
    }

    public bool TryAdd(AdventurerModel model)
    {
        if (model == null) return false;
        if (Contains(model.Id)) return false;

        data.Add(model);
        Added?.Invoke(model.Id);
        return true;
    }

    public bool TryRemove(string id)
    {
        int index = IndexOf(id);
        if (index < 0) return false;

        data.RemoveAt(index);
        Removed?.Invoke(id);
        return true;
    }

    public AdventurerModel GetById(string id)
    {
        foreach (var a in data)
            if (a.Id == id) return a;
        return null;
    }

    public bool Contains(string id) => IndexOf(id) >= 0;

    private int IndexOf(string id)
    {
        for (int i = 0; i < data.Count; i++)
            if (data[i].Id == id) return i;
        return -1;
    }
}