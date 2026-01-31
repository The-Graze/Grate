using UnityEngine;
using UnityEngine;
﻿using System.Reflection;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.GUI;
using Grate.Modules.Physics;

namespace Grate.Modules.Movement;

public class Wallrun : GrateModule
{
    public static readonly string DisplayName = "Wall Run";

    public static ConfigEntry<int> Power;
    private Vector3 baseGravity;
    private RaycastHit hit;

    private void Awake()
    {
        baseGravity = UnityEngine.Physics.gravity;
    }

    protected void FixedUpdate()
    {
        var player = GTPlayer.Instance;
        if (player.leftHand.wasColliding || player.rightHand.wasColliding)
        {
            var fieldInfo =
                typeof(GTPlayer).GetField("lastHitInfoHand", BindingFlags.NonPublic | BindingFlags.Instance);
            hit = (RaycastHit)fieldInfo.GetValue(player);
            UnityEngine.Physics.gravity = hit.normal * -baseGravity.magnitude * GravScale();
        }
        else
        {
            if (Vector3.Distance(player.bodyCollider.transform.position, hit.point) > 2 * GTPlayer.Instance.scale)
                Cleanup();
        }
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
    }

    public float GravScale()
    {
        return LowGravity.Instance.active ? LowGravity.Instance.gravityScale : Power.Value * 0.15f + 0.25f;
    }

    public static void BindConfigEntries()
    {
        Power = Plugin.ConfigFile.Bind(
            DisplayName,
            "power",
            5,
            "Wall Run Strength \n" +
            "5 means it will have normal gravity power in the direction of the last hit wall"
        );
    }

    protected override void Cleanup()
    {
        UnityEngine.Physics.gravity = baseGravity;
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Effect: Allows you to walk on any surface, no matter the angle.";
    }
}