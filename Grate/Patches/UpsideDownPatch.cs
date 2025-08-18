using System.Collections.Generic;
using GorillaLocomotion;
using HarmonyLib;
using UnityEngine;

namespace Grate.Patches;

[HarmonyPatch(typeof(VRRig), "LateUpdate")]
public class UpsideDownPatch
{
    public static List<VRRig> AffectedRigs = new();
    
    private static void Postfix(VRRig __instance)
    {
        if (!AffectedRigs.Contains(__instance))
            return;
        
        __instance.transform.Rotate(180f, 180f, 0f);
        
        __instance.leftHand.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
        __instance.rightHand.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
        __instance.head.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);

        if (__instance == VRRig.LocalRig)
        {
            __instance.head.headTransform.rotation = GTPlayer.Instance.headCollider.transform.rotation;
        }
    }
}

[HarmonyPatch(typeof(GTPlayer), "LateUpdate")]
public class UpsideDownPatchGTPlayer
{
    private static void Postfix(GTPlayer __instance)
    {
        if (!UpsideDownPatch.AffectedRigs.Contains(VRRig.LocalRig))
            return;
        
        __instance.bodyCollider.transform.Rotate(0f, 0f, 180f);
        __instance.bodyCollider.transform.localPosition = new Vector3(0f, -0.3f, 0f);
    }
}