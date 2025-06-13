using System;
using BepInEx.Configuration;
using UnityEngine;

namespace Grate.Extensions;

public static class ConfigExtensions
{
    public static ConfigValueInfo ValuesInfo(this ConfigEntryBase entry)
    {
        if (entry.SettingType == typeof(bool))
            return new ConfigValueInfo
            {
                AcceptableValues = new object[] { false, true },
                InitialValue = (bool)entry.BoxedValue ? 1 : 0
            };

        if (entry.SettingType == typeof(int))
            return new ConfigValueInfo
            {
                AcceptableValues = new object[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                InitialValue = Mathf.Clamp((int)entry.BoxedValue, 0, 10)
            };

        if (entry.SettingType == typeof(string))
        {
            var acceptableValues = ((AcceptableValueList<string>)entry.Description.AcceptableValues).AcceptableValues;
            for (var i = 0; i < acceptableValues.Length; i++)
                if (acceptableValues[i] == (string)entry.BoxedValue)
                    return new ConfigValueInfo
                    {
                        AcceptableValues = acceptableValues,
                        InitialValue = i
                    };
        }

        throw new Exception($"Unknown config type {entry.SettingType}");
    }

    public struct ConfigValueInfo
    {
        public object[] AcceptableValues;
        public int InitialValue;
    }
}