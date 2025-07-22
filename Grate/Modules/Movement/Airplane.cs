﻿using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Gestures;
using Grate.GUI;
using UnityEngine;

namespace Grate.Modules.Movement;

public class Airplane : GrateModule
{
    public static readonly string DisplayName = "Airplane";

    public static ConfigEntry<int> Speed;
    public static ConfigEntry<string> SteerWith;
    private readonly float acceleration = .1f;
    private float speedScale = 10f;

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        ReloadConfiguration();
        GestureTracker.Instance.OnGlide += OnGlide;
        Plugin.MenuController.GetComponent<Helicopter>().button.AddBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }

    private void OnGlide(Vector3 direction)
    {
        if (!enabled) return;
        var tracker = GestureTracker.Instance;
        if (
            tracker.leftTrigger.pressed ||
            tracker.rightTrigger.pressed ||
            tracker.leftGrip.pressed ||
            tracker.rightGrip.pressed) return;

        var player = GTPlayer.Instance;
        if (player.wasLeftHandColliding || player.wasLeftHandColliding) return;

        if (SteerWith.Value == "head")
            direction = player.headCollider.transform.forward;

        var rigidbody = player.bodyCollider.attachedRigidbody;
        var velocity = direction * player.scale * speedScale;
        rigidbody.velocity = Vector3.Lerp(rigidbody.velocity, velocity, acceleration);
    }

    protected override void Cleanup()
    {
        if (!MenuController.Instance.Built) return;
        if (!GestureTracker.Instance) return;
        GestureTracker.Instance.OnGlide -= OnGlide;
        Plugin.MenuController.GetComponent<Helicopter>().button.RemoveBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }

    protected override void ReloadConfiguration()
    {
        speedScale = Speed.Value * 2;
    }

    public static void BindConfigEntries()
    {
        Speed = Plugin.ConfigFile.Bind(
            DisplayName,
            "speed",
            5,
            "How fast you fly"
        );

        SteerWith = Plugin.ConfigFile.Bind(
            DisplayName,
            "steer with",
            "wrists",
            new ConfigDescription(
                "Which part of your body you use to steer",
                new AcceptableValueList<string>("wrists", "head")
            )
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "- To fly, do a T-pose (spread your arms out like wings on a plane). \n" +
               "- To fly up, point your thumbs up. \n" +
               "- To fly down, point your thumbs down.";
    }
}