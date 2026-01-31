using UnityEngine;
using UnityEngine;
﻿using System;
using GorillaLocomotion;
using Grate.Modules.Physics;
using Grate.Tools;
using HarmonyLib;

namespace Grate.Patches;

[HarmonyPatch(typeof(GTPlayer))]
[HarmonyPatch(nameof(GTPlayer.LateUpdate), MethodType.Normal)]
public class LateUpdatePatch
{
    public static Action<GTPlayer> OnLateUpdate;

    private static void Postfix(GTPlayer __instance)
    {
        try
        {
            OnLateUpdate?.Invoke(__instance);
        }
        catch
        {
        }

        Camera.main.farClipPlane = 8500;
        Camera.main.clearFlags = CameraClearFlags.Skybox;
    }
}

[HarmonyPatch(typeof(GTPlayer))]
[HarmonyPatch(nameof(GTPlayer.GetSlidePercentage), MethodType.Normal)]
public class SlidePatch
{
    private static void Postfix(GTPlayer __instance, ref float __result)
    {
        try
        {
            if (SlipperyHands.Instance)
                __result = SlipperyHands.Instance.enabled ? 1 : __result;
            if (NoSlip.Instance)
                __result = NoSlip.Instance.enabled ? 0 : __result;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}

[HarmonyPatch(typeof(VRRig))]
[HarmonyPatch(nameof(VRRig.PostTick), MethodType.Normal)]
public class VRRigLateUpdatePatch
{
    private static void Postfix(VRRig __instance, ref AudioSource ___voiceAudio)
    {
        if (!Plugin.WaWaGrazeDotCc || !___voiceAudio) return;
        try
        {
            ___voiceAudio.pitch = Mathf.Clamp(___voiceAudio.pitch, .8f, 1.2f);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}

//[HarmonyPatch(typeof(ControllerInputPoller))]
//[HarmonyPatch("Update", MethodType.Normal)]
//public class ControllerUpdatePatch
//{
//    private static void Prefix(ControllerInputPoller __instance)
//    {
//        Debug.Log("Lol");
//        GestureTracker.Instance.UpdateValues();
//    }
//}