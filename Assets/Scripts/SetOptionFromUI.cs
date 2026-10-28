using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SetOptionFromUI : MonoBehaviour
{
    public Scrollbar volumeSlider;
    [FormerlySerializedAs("turnDropdown")]
    public TMPro.TMP_Dropdown grabDropdown;   // was the turn option; turning is off in this game

    private void Start()
    {
        volumeSlider.onValueChanged.AddListener(SetGlobalVolume);

        grabDropdown.ClearOptions();
        grabDropdown.AddOptions(new List<string> { "Hold", "Toggle" });
        grabDropdown.SetValueWithoutNotify(GrabMode.Toggle ? 1 : 0);
        grabDropdown.onValueChanged.AddListener(SetGrabPlayerPref);
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
