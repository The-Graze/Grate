using HarmonyLib;
using UnityEngine;

namespace Grate.Patches;

[HarmonyPatch(typeof(VRRig), "LateUpdate")]
public class UpsideDownPatch
{
    public static bool Enabled;
    
    private static void Postfix(VRRig __instance)
    {
        if (!Enabled || __instance.isLocal)
            return;
        
        Quaternion oldRotation = __instance.transform.rotation;
        oldRotation.x *= 180f;
        __instance.transform.rotation = oldRotation;
        
        __instance.head.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
        __instance.rightHand.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
        __instance.leftHand.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
    }
}