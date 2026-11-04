using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SetOptionFromUI : MonoBehaviour
{
    public Scrollbar volumeSlider;
    [FormerlySerializedAs("turnDropdown")]
    public TMPro.TMP_Dropdown grabDropdown;   // was the turn option; turning is off in this game

    public TMPro.TMP_Dropdown teleportDropdown;
    public TMPro.TMP_Dropdown comfortDropdown;

    private void Start()
    {
        volumeSlider.onValueChanged.AddListener(SetGlobalVolume);

        // "Sticky": press the grip once to pick a tool up and it stays in your hand until you press again
        grabDropdown.ClearOptions();
        grabDropdown.AddOptions(new List<string> { "Hold", "Sticky" });
        grabDropdown.SetValueWithoutNotify(GrabMode.Toggle ? 1 : 0);
        grabDropdown.onValueChanged.AddListener(SetGrabPlayerPref);

        if (teleportDropdown)
        {
            teleportDropdown.ClearOptions();
            teleportDropdown.AddOptions(new List<string> { "Right stick", "Both sticks" });
            teleportDropdown.SetValueWithoutNotify(ControlOptions.LeftStickTeleport ? 1 : 0);
            teleportDropdown.onValueChanged.AddListener(v => ControlOptions.LeftStickTeleport = v == 1);
        }

        // Comfort: a quick blink to black on every teleport, for players who feel the jump
        if (comfortDropdown)
        {
            comfortDropdown.ClearOptions();
            comfortDropdown.AddOptions(new List<string> { "Off", "On" });
            comfortDropdown.SetValueWithoutNotify(ControlOptions.ComfortBlink ? 1 : 0);
            comfortDropdown.onValueChanged.AddListener(v => ControlOptions.ComfortBlink = v == 1);
        }
    }

    public void SetGlobalVolume(float value)
    {
        AudioListener.volume = value;
    }

    public void SetGrabPlayerPref(int value)
    {
        GrabMode.Toggle = value == 1;
    }
}
