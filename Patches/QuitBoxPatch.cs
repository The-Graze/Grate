﻿using GorillaLocomotion;
using HarmonyLib;
using UnityEngine;
using Grate.Extensions;

namespace Grate.Patches
{
    [HarmonyPatch(typeof(GorillaQuitBox))]
    [HarmonyPatch("OnBoxTriggered", MethodType.Normal)]
    class QuitBoxPatch
    {
        private static bool Prefix()
        {
            Player.Instance.TeleportTo(new Vector3(-66.4845f, 11.7564f, -82.5688f), Quaternion.Euler(Vector3.zero));
                return !Plugin.WaWa_graze_dot_cc;
        }
    }
}