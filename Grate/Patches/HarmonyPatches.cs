using UnityEngine;
using UnityEngine;
﻿using System.Reflection;
using HarmonyLib;

namespace Grate;

/// <summary>
///     This class handles applying harmony patches to the game.
///     You should not need to modify this class.
/// </summary>
public class HarmonyPatches
{
    public const string InstanceId = PluginInfo.Guid;
    private static Harmony instance;

    public static bool IsPatched { get; private set; }

    internal static void ApplyHarmonyPatches()
    {
        if (!IsPatched)
        {
            if (instance == null) instance = new Harmony(InstanceId);

            instance.PatchAll(Assembly.GetExecutingAssembly());
            IsPatched = true;
        }
    }

    internal static void RemoveHarmonyPatches()
    {
        if (instance != null && IsPatched)
        {
            instance.UnpatchSelf();
            IsPatched = false;
        }
    }
}