using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Grate.Patches;

[HarmonyPatch]
public class VRRigCachePatches
{
    public static Action<NetPlayer, VRRig> OnRigCached;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        return new MethodBase[]
        {
            AccessTools.Method("VRRigCache:RemoveRigFromGorillaParent")
        };
    }

    private static void Postfix(NetPlayer player, VRRig vrrig)
    {
        OnRigCached?.Invoke(player, vrrig);
    }
}