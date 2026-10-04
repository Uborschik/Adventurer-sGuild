using Godot;

public class SingleSelection<T> where T : Button
{
    private T current;

    public T Current => current;

    public bool OnToggled(T item, bool pressed)
    {
        if (pressed)
        {
            if (current == item) return false;

            var prev = current;
            current = item;

            if (prev != null)
                prev.ButtonPressed = false;

            return true;
        }

        if (current == item)
        {
            current = null;
            return true;
        }

        return false;
    }

    public void Deselect()
    {
        if (current == null) return;

        var prev = current;
        current = null;
        prev.ButtonPressed = false;
    }

    public void Forget(T item)
    {
        if (current == item) current = null;
    }
}