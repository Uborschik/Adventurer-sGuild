using System;
using System.Collections.Generic;

namespace AdventurersGuild.Core;

public class Pool<T> where T : class
{
    private readonly Queue<T> available = new();
    private readonly Func<T> factory;
    private readonly int maxSize;

    private int liveCount;

    public Pool(Func<T> factory, int initialSize = 0, int maxSize = 0)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.maxSize = maxSize;

        if (maxSize > 0 && initialSize > maxSize)
            throw new ArgumentException(
                $"initialSize ({initialSize}) не может превышать maxSize ({maxSize})");

        for (int i = 0; i < initialSize; i++)
        {
            available.Enqueue(factory());
        }
    }

    public int AvailableCount => available.Count;
    public int LiveCount => liveCount;

    public T Get()
    {
        if (available.Count > 0)
        {
            liveCount++;
            return available.Dequeue();
        }

        if (maxSize > 0 && liveCount >= maxSize)
            throw new InvalidOperationException(
                $"Пул {typeof(T).Name} переполнен (лимит: {maxSize})");

        liveCount++;
        return factory();
    }

    public void Return(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (maxSize > 0 && available.Count >= maxSize)
        {
            liveCount--;
            return;
        }

        liveCount--;
        available.Enqueue(item);
    }

    public void Clear()
    {
        liveCount -= available.Count;
        available.Clear();
    }
}