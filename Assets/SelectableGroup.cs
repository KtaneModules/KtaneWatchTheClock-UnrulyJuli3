public sealed class SelectableGroup
{
    private DigitGroup _group;

    private readonly string _modName;

    private readonly Selectable _root;

    private readonly BombComponent _module;

    private bool _isUnhooked = false;

    private Selectable[] _lastSelectables;

    public SelectableGroup(DigitGroup group, string modName, Selectable root)
    {
        _group = group;
        _modName = modName;
        _root = root;
        _module = _root.GetComponent<BombComponent>();
    }

    public void Hook()
    {
        _group.Log("Hooking {0}", _root.name);
        _lastSelectables = _root.Children;
        foreach (Selectable selectable in _lastSelectables)
        {
            if (selectable)
            {
                selectable.OnInteract -= HandleInteraction;
                selectable.OnInteract += HandleInteraction;
                //_group.Log("Assigned handler to selectable {0} of module {1}", selectable.name, _modName);
            }
            else
            {
                _group.Log("{0} is missing a selectable child", _modName);
            }
        }
    }

    public void Unhook()
    {
        if (_isUnhooked)
            return;

        _group.Log("Unhooking {0}", _root.name);
        _isUnhooked = true;
        foreach (Selectable selectable in _lastSelectables)
            if (selectable)
                selectable.OnInteract -= HandleInteraction;
    }

    public void Update()
    {
        if (_isUnhooked)
            return;

        if (_module.IsSolved)
        {
            Unhook();
            return;
        }

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
