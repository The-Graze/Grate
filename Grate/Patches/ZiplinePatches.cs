using UnityEngine;
using UnityEngine;
﻿using System;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Gameplay;
using Grate.Modules.Movement;
using Grate.Tools;
using HarmonyLib;

namespace Grate.Patches;

[HarmonyPatch(typeof(GorillaZipline))]
[HarmonyPatch(nameof(GorillaZipline.Update), MethodType.Normal)]
public class ZiplineUpdatePatch
{
    private static void Postfix(GorillaZipline __instance, BezierSpline ___spline, float ___currentT,
        GorillaHandClimber ___currentClimber)
    {
        if (!Plugin.WaWaGrazeDotCc) return;
        try
        {
            var rockets = Rockets.Instance;
            if (!rockets || !rockets.enabled || !___currentClimber) return;
            var curDir = __instance.GetCurrentDirection();
            var rocketDir = rockets.AddedVelocity();
            var currentSpeed = Traverse.Create(__instance).Property("currentSpeed");
            var speedDelta = Vector3.Dot(curDir, rocketDir) * Time.deltaTime * rocketDir.magnitude * 1000f;
            currentSpeed.SetValue(currentSpeed.GetValue<float>() + speedDelta);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}