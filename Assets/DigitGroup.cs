using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class DigitGroup
{
    private static readonly Dictionary<string, DigitGroup> s_groups = new Dictionary<string, DigitGroup>();

    private string _groupId;

    private readonly KtaneTimerDigit _author;

    private readonly List<KtaneTimerDigit> _mods = new List<KtaneTimerDigit>();

    private readonly List<SelectableGroup> _selectableGroups = new List<SelectableGroup>();

    private KtaneTimerDigit[] Modules
    {
        get
        {
            return _mods.Concat(new[] { _author }).ToArray();
        }
    }

    public DigitGroup(string groupId, KtaneTimerDigit author)
    {
        _groupId = groupId;
        _author = author;
        Log("Initialized");
    }

    public static DigitGroup GetDigitGroup(string groupId, KtaneTimerDigit mod)
    {
        if (s_groups.ContainsKey(groupId))
        {
            DigitGroup group = s_groups[groupId];
            group.AddModule(mod);
            return group;
        }

        return new DigitGroup(groupId, mod);
    }

    public int GetStartingDigit(KtaneTimerDigit mod)
    {
        List<int> possibleDigits = Enumerable.Range(0, 10).ToList();
        possibleDigits.Remove(0); // always allow 0 to be free upon spawn
        foreach (KtaneTimerDigit otherMod in Modules)
            if (otherMod != mod)
                possibleDigits.Remove(otherMod.Digit);

        return possibleDigits[Random.Range(0, possibleDigits.Count)];
    }

    public bool IsAuthor(KtaneTimerDigit mod)
    {
        return mod == _author;
    }

    public void AddModule(KtaneTimerDigit mod)
    {
        _mods.Add(mod);
        Log("Module added (total {0})", Modules.Length);
    }

    public void Update()
    {
        foreach (SelectableGroup group in _selectableGroups)
            group.Update();
    }

    public void Activate()
    {
        Hook(_author.transform);
    }

    public void Destroy()
    {
        Log("Removing group");
        s_groups.Remove(_groupId);
    }
    
    private void Hook(Transform mod)
    {
        if (Application.isEditor)
            return;

        int childCount = mod.parent.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = mod.parent.GetChild(i);
            HookModule(child);
        }
    }

    private void HookModule(Transform child)
    {
        string modName;

        var mod = child.GetComponent<ModBombComponent>();
        if (mod)
        {
            string name = mod.GetModuleDisplayName(),
                id = mod.GetModComponentType();

            if (id == _author._module.ModuleType || _author._ignoredModules.Contains(name))
                return;

            modName = name;
        }
        else
        {
            var vanillaComponent = child.GetComponent<BombComponent>();
            if (!vanillaComponent)
                return;

            modName = vanillaComponent.ComponentType.ToString();
        }

        Log("Found a module: {0}", modName);

        var rootSelectable = child.GetComponent<Selectable>();
        if (!rootSelectable)
        {
            Log("Module {0} has no root Selectable, ignoring", modName);
            return;
        }

        SelectableGroup group = new SelectableGroup(this, modName, rootSelectable);
        group.Hook();
        _selectableGroups.Add(group);
    }

    internal void HandleInteraction(string modName)
    {
        int timerDigit = (int)(_author._bomb.GetTime() % 10f);
        foreach (KtaneTimerDigit mod in _mods)
            if (mod.Digit == timerDigit)
            {
                mod._module.HandleStrike();
                mod.Log("Strike! Interacted with {0} while last timer digit was {1}", modName, timerDigit);
                return;
            }
    }

    internal void Log(string format, params object[] args)
    {
        Debug.LogFormat("[DigitGroup {0}] {1}", _groupId, string.Format(format, args));
    }
}