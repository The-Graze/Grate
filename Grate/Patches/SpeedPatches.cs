using System;
using GorillaLocomotion;
using Grate.Modules;
using Grate.Tools;
using HarmonyLib;
using UnityEngine;

namespace Grate.Patches;

[HarmonyPatch(typeof(GorillaTagManager))]
[HarmonyPatch("LocalPlayerSpeed", MethodType.Normal)]
internal class TagSpeedPatch
{
    private static void Postfix(GorillaTagManager __instance, ref float[] __result)
    {
        try
        {
            if (!SpeedBoost.active) return;

            for (var i = 0; i < __result.Length; i++)
                __result[i] *= SpeedBoost.scale;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}

[HarmonyPatch(typeof(GorillaGameManager))]
[HarmonyPatch("LocalPlayerSpeed", MethodType.Normal)]
internal class GenericSpeedPatch
{
    private static void Postfix(GorillaGameManager __instance, ref float[] __result)
    {
        try
        {
            if (!SpeedBoost.active) return;

            for (var i = 0; i < __result.Length; i++)
                __result[i] *= SpeedBoost.scale;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}

[HarmonyPatch(typeof(GorillaPaintbrawlManager))]
[HarmonyPatch("LocalPlayerSpeed", MethodType.Normal)]
internal class BattleSpeedPatch
{
    private static void Postfix(GorillaPaintbrawlManager __instance, ref float[] __result)
    {
        try
        {
            if (!SpeedBoost.active) return;

            for (var i = 0; i < __result.Length; i++)
                __result[i] *= SpeedBoost.scale;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}

[HarmonyPatch(typeof(GorillaHuntManager))]
[HarmonyPatch("LocalPlayerSpeed", MethodType.Normal)]
internal class HuntSpeedPatch
{
    private static void Postfix(GorillaHuntManager __instance, ref float[] __result)
    {
        try
        {
            if (!SpeedBoost.active) return;

            for (var i = 0; i < __result.Length; i++)
                __result[i] *= SpeedBoost.scale;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}

[HarmonyPatch(typeof(GTPlayer))]
[HarmonyPatch("GetSwimmingVelocityForHand", MethodType.Normal)]
internal class SwimmingVelocityPatch
{
    private static void Postfix(ref Vector3 swimmingVelocityChange)
    {
        try
        {
            if (!SpeedBoost.active) return;
            swimmingVelocityChange *= SpeedBoost.scale;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}