using GorillaNetworking;
using UnityEngine;
using UnityEngine;
﻿using HarmonyLib;

namespace Grate.Patches;

[HarmonyPatch(typeof(GorillaQuitBox))]
[HarmonyPatch(nameof(GorillaQuitBox.OnBoxTriggered), MethodType.Normal)]
internal class QuitBoxPatch
{
    private static bool Prefix()
    {
        TeleportPatch.TeleportPlayer(new Vector3(-66.4845f, 11.7564f, -82.5688f), 0);
        foreach (var wawa in PhotonNetworkController.Instance.enableOnStartup) wawa.SetActive(true);
        foreach (var wawa2 in PhotonNetworkController.Instance.disableOnStartup) wawa2.SetActive(false);
        return !Plugin.WaWaGrazeDotCc;
    }
}