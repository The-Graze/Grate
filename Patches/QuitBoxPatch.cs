using GorillaLocomotion;
using HarmonyLib;
using UnityEngine;

namespace Grate.Patches
{
    [HarmonyPatch(typeof(GorillaQuitBox))]
    [HarmonyPatch("OnBoxTriggered", MethodType.Normal)]
    class QuitBoxPatch
    {
        private static bool Prefix()
        {
            if (Plugin.InModded)
            {
                Player.Instance.TeleportTo(new Vector3(-66.4845f, 11.7564f, - 82.5688f), Quaternion.Euler(Vector3.zero));
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}