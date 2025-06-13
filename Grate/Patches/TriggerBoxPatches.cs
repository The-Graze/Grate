using GorillaNetworking;
using Grate.Modules.Physics;
using Grate.Tools;
using HarmonyLib;

namespace Grate.Patches;

internal class TriggerBoxPatches
{
    public static bool triggersEnabled = true;

    [HarmonyPatch(typeof(GorillaGeoHideShowTrigger))]
    [HarmonyPatch("OnBoxTriggered", MethodType.Normal)]
    internal class GeoTriggerPatches
    {
        private static bool Prefix()
        {
            return triggersEnabled;
        }
    }

    [HarmonyPatch(typeof(GorillaNetworkDisconnectTrigger))]
    [HarmonyPatch("OnBoxTriggered", MethodType.Normal)]
    internal class DisconnectTriggerPatches
    {
        private static bool Prefix()
        {
            return triggersEnabled;
        }
    }

    [HarmonyPatch(typeof(GorillaNetworkJoinTrigger))]
    [HarmonyPatch("OnBoxTriggered", MethodType.Normal)]
    internal class JoinTriggerPatches
    {
        private static bool Prefix()
        {
            return triggersEnabled;
        }
    }

    [HarmonyPatch(typeof(GorillaQuitBox))]
    [HarmonyPatch("OnBoxTriggered", MethodType.Normal)]
    internal class QuitTriggerPatches
    {
        private static bool Prefix()
        {
            if (!triggersEnabled)
            {
                Logging.Debug("Player fell out of map, disabling noclip");
                NoClip.Instance.enabled = false;
            }

            return triggersEnabled;
        }
    }

    [HarmonyPatch(typeof(GorillaSetZoneTrigger))]
    [HarmonyPatch("OnBoxTriggered", MethodType.Normal)]
    internal class ZoneTriggerPatches
    {
        private static bool Prefix()
        {
            return triggersEnabled;
        }
    }

    [HarmonyPatch(typeof(GorillaKeyboardButton))]
    [HarmonyPatch("OnButtonPressedEvent", MethodType.Normal)]
    internal class KeyboardButtonPatches
    {
        private static bool Prefix()
        {
            return triggersEnabled;
        }
    }
}