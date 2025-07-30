using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Grate;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Interaction;
using Grate.Modules;
using Grate.Tools;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPage : MonoBehaviour
{
    private ConfigEntryBase entry;
    private GrateOptionWheel modSelector, configSelector;
    private GrateSlider valueSlider;

    private void Awake()
    {
        try
        {
            modSelector = transform.Find("Mod Selector").gameObject.AddComponent<GrateOptionWheel>();
            modSelector.InitializeValues(GetModulesWithSettings());

            configSelector = transform.Find("Config Selector").gameObject.AddComponent<GrateOptionWheel>();
            configSelector.InitializeValues(GetConfigKeys(modSelector.Selected));

            valueSlider = transform.Find("Value Slider").gameObject.AddComponent<GrateSlider>();
            entry = GetEntry(modSelector.Selected, configSelector.Selected);
            var info = entry.ValuesInfo();
            valueSlider.InitializeValues(info.AcceptableValues, info.InitialValue);

            modSelector.OnValueChanged += mod => { configSelector.InitializeValues(GetConfigKeys(mod)); };

            configSelector.OnValueChanged += config =>
            {
                entry = GetEntry(modSelector.Selected, configSelector.Selected);
                UpdateText();
                var info = entry.ValuesInfo();
                valueSlider.InitializeValues(info.AcceptableValues, info.InitialValue);
            };

            valueSlider.OnValueChanged += value => { entry.BoxedValue = value; };
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private ConfigEntryBase GetEntry(string modName, string key)
    {
        foreach (var definition in Plugin.ConfigFile.Keys)
            if (definition.Section == modName && definition.Key == key)
                return Plugin.ConfigFile[definition];

        throw new Exception($"Could not find config entry for {modName} with key {key}");
    }

    private List<string> GetConfigKeys(string modName)
    {
        try
        {
            var configKeys = new List<string>();
            foreach (var definition in Plugin.ConfigFile.Keys)
                if (definition.Section == modName)
                    configKeys.Add(Plugin.ConfigFile[definition].Definition.Key);

            return configKeys;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
            return null;
        }
    }

    private List<string> GetModulesWithSettings()
    {
        try
        {
            var modulesWithSettings = new List<string> { "General" };
            foreach (var type in GrateModule.GetGrateModuleTypes())
            {
                if (type == typeof(GrateModule)) continue;
                var bindConfigs = type.GetMethod("BindConfigEntries");
                if (bindConfigs is null) continue;

                var nameField = type.GetField("DisplayName");
                var displayName = (string)nameField.GetValue(null);
                modulesWithSettings.Add(displayName);
            }

            return modulesWithSettings;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
            return null;
        }
    }

    public void UpdateText()
    {
        if (entry is null) return;
        MenuController.Instance.helpText.text =
            $"{modSelector.Selected} > {configSelector.Selected}\n" +
            "-----------------------------------\n" +
            entry.Description.Description +
            $"\n\nDefault: {entry.DefaultValue}";
    }
}

public class GrateOptionWheel : MonoBehaviour
{
    private string _selected;
    private Transform cylinder;
    private Text[] labels;
    public Action<string> OnValueChanged;
    private int selectedValue, selectedLabel;
    private ButtonController upButton, downButton;
    private List<string> values;

    public string Selected
    {
        get => _selected;
        private set
        {
            _selected = value;
            OnValueChanged?.Invoke(value);
        }
    }

    private void Awake()
    {
        try
        {
            cylinder = transform.Find("Cylinder");
            labels = cylinder.GetComponentsInChildren<Text>();
            upButton = transform.Find("Arrow Up").gameObject.AddComponent<ButtonController>();
            downButton = transform.Find("Arrow Down").gameObject.AddComponent<ButtonController>();
            upButton.buttonPushDistance = .01f;
            downButton.buttonPushDistance = .01f;

            upButton.OnPressed += (button, pressed) =>
            {
                Cycle(-1);
                button.IsPressed = false;
            };

            downButton.OnPressed += (button, pressed) =>
            {
                Cycle(1);
                button.IsPressed = false;
            };
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void FixedUpdate()
    {
        try
        {
            var angle = selectedLabel % 6 * 60f;
            cylinder.localRotation = Quaternion.Slerp(
                cylinder.localRotation,
                Quaternion.Euler(angle, 0, 0),
                Time.fixedDeltaTime * 10f
            );
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void Cycle(int direction)
    {
        try
        {
            selectedValue = MathExtensions.Wrap(selectedValue + direction, 0, values.Count);
            selectedLabel = MathExtensions.Wrap(selectedLabel + direction, 0, labels.Length);
            Selected = values[selectedValue];
            var labelToUpdate = MathExtensions.Wrap(selectedLabel + 2 * direction, 0, labels.Length);
            var newLabel = values[MathExtensions.Wrap(selectedValue + 2 * direction, 0, values.Count)];
            labels[labelToUpdate].text = newLabel;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    public void InitializeValues(List<string> values)
    {
        try
        {
            selectedLabel = 0;
            selectedValue = 0;
            this.values = values;
            Selected = values[selectedValue];
            for (var i = 0; i < labels.Length; i++)
            {
                int value;
                if (i < labels.Length / 2)
                    value = MathExtensions.Wrap(selectedValue + i, 0, values.Count);
                else
                    value = MathExtensions.Wrap(values.Count - labels.Length + i, 0, values.Count);

                var label = MathExtensions.Wrap(selectedLabel + i, 0, labels.Length);
                labels[label].text = values[value];
            }
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}

public class GrateSlider : MonoBehaviour
{
    private Knob _knob;
    private object _selected;
    private Transform knob;
    private Text label;
    public Action<object> OnValueChanged;
    private int selectedValue;
    private Transform sliderEnd;
    private Transform? sliderStart;
    private object[] values;

    public object Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            OnValueChanged?.Invoke(value);
        }
    }


    private void Awake()
    {
        try
        {
            sliderStart = transform.Find("Start");
            sliderEnd = transform.Find("End");
            knob = transform.Find("Knob");
            label = GetComponentInChildren<Text>();
            _knob = knob.gameObject.AddComponent<Knob>();
            _knob.Initialize(sliderStart, sliderEnd);
            _knob.OnValueChanged += value =>
            {
                selectedValue = value;
                Selected = values[selectedValue];
                label.text = Selected.ToString();
            };
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    public void InitializeValues(object[] values, int initialValue)
    {
        try
        {
            this.values = values;
            selectedValue = initialValue;
            Selected = values[initialValue];
            label.text = Selected.ToString();
            _knob.divisions = values.Length - 1;
            _knob.Value = initialValue;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}

public class Knob : GrateInteractable
{
    public int divisions;
    public Action<int>? OnValueChanged;
    private Transform? start, end;
    private int value;

    public int Value
    {
        get => value;
        set
        {
            if (value != this.value)
            {
                OnValueChanged?.Invoke(value);
                if (Selected)
                    GestureTracker.Instance.HapticPulse(selectors[0].IsLeft);
                Sounds.Play(Sounds.Sound.keyboardclick);
            }

            this.value = value;
            transform.position = Vector3.Lerp(start.position, end.position, (float)Value / divisions);
        }
    }

    private void FixedUpdate()
    {
        if (!Selected) return;

        // Get the length of the projection of the start-to-hand vector onto the start-to-end vector
        var startToHand = selectors[0].transform.position - start.position;
        var startToEnd = end.position - start.position;
        var projLength = Vector3.Dot(startToEnd, startToHand) / startToEnd.magnitude;

        // Get the ratio of the projection to the length of the start-to-end vector
        projLength = Mathf.Clamp01(projLength / startToEnd.magnitude);
        // Get the index of the division that the hand is closest to
        Value = Mathf.RoundToInt(projLength * divisions);
    }

    public void Initialize(Transform? start, Transform end)
    {
        priority = MenuController.Instance.priority;
        this.start = start;
        this.end = end;
    }
}