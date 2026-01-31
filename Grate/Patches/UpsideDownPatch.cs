using UnityEngine;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using GorillaLocomotion;
using HarmonyLib;

namespace Grate.Patches;

[HarmonyPatch(typeof(VRRig), nameof(VRRig.PostTick))]
public class UpsideDownPatch
{
    public static Dictionary<VRRig?, bool> AffectedRigs = [];

    // ReSharper disable once InconsistentNaming
    private static void Postfix(VRRig __instance)
    {
        if (!AffectedRigs.Keys.Contains(__instance))
            return;
        if (!AffectedRigs[__instance])
        {
            __instance.transform.Rotate(180f, 180f, 0f);
            AffectedRigs[__instance] = true;
        }


        if (!__instance.isLocal) return;

        __instance.leftHand.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
        __instance.rightHand.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
        __instance.head.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);

        __instance.head.rigTarget.rotation = GTPlayer.Instance.headCollider.transform.rotation;
    }
}

[HarmonyPatch(typeof(GTPlayer), nameof(GTPlayer.LateUpdate))]
public class UpsideDownPatchGtPlayer
{
    // ReSharper disable once InconsistentNaming
    private static void Postfix(GTPlayer __instance)
    {
        if (!UpsideDownPatch.AffectedRigs.Keys.Contains(VRRig.LocalRig))
            return;

        __instance.bodyCollider.transform.Rotate(0f, 0f, 180f);
        __instance.bodyCollider.transform.localPosition = new Vector3(0f, -0.3f, 0f);
    }
}