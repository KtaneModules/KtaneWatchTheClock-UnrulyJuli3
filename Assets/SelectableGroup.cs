public sealed class SelectableGroup
{
    private DigitGroup _group;

    private readonly string _modName;

    private readonly Selectable _root;

    private Selectable[] _lastSelectables;

    public SelectableGroup(DigitGroup group, string modName, Selectable root)
    {
        _group = group;
        _modName = modName;
        _root = root;
    }

    public void Hook()
    {
        _lastSelectables = _root.Children;
        foreach (Selectable selectable in _lastSelectables)
        {
            if (selectable)
            {
                selectable.OnInteract -= HandleInteraction;
                selectable.OnInteract += HandleInteraction;
            }
            else
            {
                _group.Log("Module {0} is missing a selectable child", _modName);
            }
        }
    }

    public void Update()
    {
        Selectable[] current = _root.Children;
        if (current.Length != _lastSelectables.Length)
        {
            // a selectable(s) was added or removed! re-hook everything
            Hook();
            return;
        }

        for (int i = 0; i < _lastSelectables.Length; i++)
            if (_lastSelectables[i] != current[i])
            {
                // this selectable is different! re-hook everything
                Hook();
                return;
            }
    }

    private bool HandleInteraction()
    {
        _group.HandleInteraction(_modName);
        return false;
    }
}
