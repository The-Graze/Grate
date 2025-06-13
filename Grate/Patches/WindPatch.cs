using Grate.Modules.Misc;
using HarmonyLib;
using UnityEngine;

namespace Grate.Patches;

[HarmonyPatch(typeof(ForceVolume))]
[HarmonyPatch("SliceUpdate", MethodType.Normal)]
internal class WindPatch
{
    private static bool Prefix(ForceVolume __instance)
    {
        if (DisableWind.Enabled)
        {
            if (__instance.audioSource != null) __instance.audioSource.enabled = false;

            var volume = Traverse.Create(__instance).Field<Collider>("volume").Value;
            if (volume != null) volume.enabled = false;

            return false;
        }

        var volume2 = Traverse.Create(__instance).Field<Collider>("volume").Value;
        if (volume2 != null) volume2.enabled = true;
        return true;
    }
}