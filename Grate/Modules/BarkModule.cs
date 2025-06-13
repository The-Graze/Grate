using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using Grate.Networking;
using Grate.Tools;
using UnityEngine;

namespace Grate.Modules;

public abstract class GrateModule : MonoBehaviour
{
    public static GrateModule LastEnabled;
    public static Dictionary<string, bool> enabledModules = new();
    public static string enabledModulesKey = "GrateEnabledModules";
    public ButtonController button;
    public List<ConfigEntryBase> ConfigEntries;

    protected virtual void Start()
    {
        enabled = false;
    }

    protected virtual void OnEnable()
    {
        LastEnabled = this;
        Plugin.configFile.SettingChanged += SettingsChanged;
        if (button)
            button.IsPressed = true;
        SetStatus(true);
    }

    protected virtual void OnDisable()
    {
        Plugin.configFile.SettingChanged -= SettingsChanged;
        if (button)
            button.IsPressed = false;
        Cleanup();
        SetStatus(false);
    }

    protected virtual void OnDestroy()
    {
        Cleanup();
    }

    protected virtual void ReloadConfiguration()
    {
    }

    public abstract string GetDisplayName();

    protected void SettingsChanged(object sender, SettingChangedEventArgs e)
    {
        foreach (var field in GetType().GetFields())
            if (e.ChangedSetting == field.GetValue(this))
                ReloadConfiguration();
    }

    public abstract string Tutorial();

    protected abstract void Cleanup();

    public void SetStatus(bool enabled)
    {
        var name = GetDisplayName();
        if (enabledModules.ContainsKey(name))
            enabledModules[name] = enabled;
        else
            enabledModules.Add(name, enabled);
        NetworkPropertyHandler.Instance?.ChangeProperty(enabledModulesKey, enabledModules);
    }

    public static List<Type> GetGrateModuleTypes()
    {
        try
        {
            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(GrateModule).IsAssignableFrom(t))
                .ToList();
            types.Sort((x, y) =>
            {
                var xField = x.GetField("DisplayName", BindingFlags.Public | BindingFlags.Static);
                var yField = y.GetField("DisplayName", BindingFlags.Public | BindingFlags.Static);

                if (xField == null || yField == null)
                    return 0;

                var xValue = (string)xField.GetValue(null);
                var yValue = (string)yField.GetValue(null);

                return string.Compare(xValue, yValue);
            });
            return types;
        }
        catch (ReflectionTypeLoadException ex)
        {
            Logging.Exception(ex);
            Logging.Warning("Inner exceptions:");
            foreach (var inner in ex.LoaderExceptions) Logging.Exception(inner);
        }

        return null;
    }
}