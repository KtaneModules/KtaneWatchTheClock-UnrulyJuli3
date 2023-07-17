using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;

public class KtaneTimerDigit : MonoBehaviour
{
	[SerializeField]
	internal KMBombModule _module;

	[SerializeField]
	internal KMBombInfo _bomb;

    [SerializeField]
    private KMAudio _audio;

	[SerializeField]
	private KMBossModule _boss;

	[SerializeField]
	private TextMesh _digitText;

    [SerializeField]
	private KMSelectable _button;

	[SerializeField]
	private Animator _buttonAnimator;

	private static int s_loggingId;

	private int _loggingNum = ++s_loggingId;

	private DigitGroup _digitGroup;

	private int _digit = -1;

	private bool _isActive = false;

	private bool _isInSolvableState = false;

	private bool _isSolved = false;

	internal string[] _ignoredModules;

	public int Digit
	{
		get
		{
			return _digit;
		}
	}

	private void Start()
	{
		_ignoredModules = _boss.GetIgnoredModules(_module.ModuleDisplayName, new string[] { _module.ModuleDisplayName });

        _digitGroup = DigitGroup.GetDigitGroup(_bomb.GetSerialNumber(), this);
		_digit = _digitGroup.GetStartingDigit(this);

		_module.OnActivate += OnActivate;

		_button.OnInteract += OnButtonDown;
        _button.OnInteractEnded += OnButtonUp;

		_digitText.text = "";
        Log("Starting digit: {0}", _digit);
    }

	private void OnDestroy()
	{
		if (_digitGroup.IsAuthor(this))
			_digitGroup.Destroy();
	}

	private void EnterSolvableState()
	{
		_isInSolvableState = true;
		_digitText.text = "e";
		_digitText.color = Color.cyan;
	}

	private void Update()
	{
        if (_isSolved || _isInSolvableState)
            return;

		List<string> remaining = _bomb.GetSolvableModuleNames(),
			solved = _bomb.GetSolvedModuleNames();

		foreach (string name in solved)
			remaining.Remove(name);

		if (remaining.Count(name => !_ignoredModules.Contains(name)) <= 0)
		{
            EnterSolvableState();
            Log("All non-ignored modules solved. Module is now solvable.");
            return;
        }

		if (_digitGroup.IsAuthor(this))
			_digitGroup.Update();
	}

	private void OnActivate()
	{
		_isActive = true;
		UpdateDigit();
		if (_digitGroup.IsAuthor(this))
            _digitGroup.Activate();
	}

	internal void UpdateDigit()
	{
		if (!_isInSolvableState)
			_digitText.text = _digit.ToString();
    }

	private bool OnButtonDown()
	{
		HandleButtonPress();
        return false;
	}

	private void HandleButtonPress(int forceDigit = -1)
	{
        if (_isActive)
        {
            _button.AddInteractionPunch(0.75f);
            _audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _button.transform);
            _buttonAnimator.SetBool("IsPressed", true);

            if (!_isSolved)
            {
                if (_isInSolvableState)
                {
                    _isSolved = true;
                    _module.HandlePass();
                    Log("Button pressed. Module solved.");
                }
                else
                {
                    _digit = forceDigit > -1 ? forceDigit :(_digit + 1) % 10;
                    UpdateDigit();
                    Log("Button pressed. Digit changed to: {0}", _digit);
                }
            }
        }
    }

	private void HandleFullButtonPress(int forceDigit = -1)
	{
		HandleButtonPress(forceDigit);
		OnButtonUp();
	}

	private void OnButtonUp()
	{
        if (_isActive)
		{
            _audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, _button.transform);
            _buttonAnimator.SetBool("IsPressed", false);
        }
    }

    internal void Log(string format, params object[] args)
	{
		Debug.LogFormat("[{0} #{1}] {2}", _module.ModuleDisplayName, _loggingNum, string.Format(format, args));
	}

	private static readonly Regex s_commandRegex = new Regex("^(?:[a-z]+)(?: (\\d+))?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} press | !{0} digit 7 (jump to specific digit)";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
		Match match = s_commandRegex.Match(command);
		if (match.Success)
		{
			yield return null;

			if (_isInSolvableState)
			{
				HandleFullButtonPress();
			}
			else
			{
                int forceDigit = -1;

                Group group = match.Groups[1];
                if (group.Success)
                {
                    int digitNum;
                    if (int.TryParse(group.Value, out digitNum))
                    {
                        if (digitNum >= 0 && digitNum <= 9)
                            forceDigit = digitNum;
                    }
                }

                HandleFullButtonPress(forceDigit);
            }
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
		if (_isSolved)
			yield break;

		if (!_isInSolvableState)
			EnterSolvableState();

		HandleFullButtonPress();
    }
}
