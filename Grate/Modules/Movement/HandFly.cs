using UnityEngine;
using UnityEngine;
﻿using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.GUI;

namespace Grate.Modules.Movement;

public class LocalGorillaVelocityTracker : MonoBehaviour
{
    private Vector3 previousLocalPosition;
    private Vector3 velocity;

    private void Start()
    {
        previousLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        var localDisplacement = transform.localPosition - previousLocalPosition;
        var localVelocity = localDisplacement / Time.deltaTime;

        velocity = transform.parent.TransformDirection(localVelocity);

        previousLocalPosition = transform.localPosition;
    }

    public Vector3 GetVelocity()
    {
        return velocity;
    }
}

public class HandFly : GrateModule
{
    public static string DisplayName = "Hand Fly";

    private static ConfigEntry<int>? Speed;
    private LocalGorillaVelocityTracker? left;
    private LocalGorillaVelocityTracker? right;

    //used desmos for ts
    private float SpeedScale => Speed!.Value * 2.5f + 10;

    private void FixedUpdate()
    {
        if (ControllerInputPoller.instance.leftControllerIndexFloat > 0.5f)
            GorillaTagger.Instance.rigidbody.velocity -= right!.GetVelocity() / SpeedScale * GTPlayer.Instance.scale;

        if (ControllerInputPoller.instance.rightControllerIndexFloat > 0.5f)
            GorillaTagger.Instance.rigidbody.velocity -= left!.GetVelocity() / SpeedScale * GTPlayer.Instance.scale;
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built || !enabled) return;
        Plugin.MenuController?.GetComponent<Fly>().button.AddBlocker(ButtonController.Blocker.MOD_INCOMPAT);
        right = GTPlayer.Instance.LeftHand.controllerTransform.AddComponent<LocalGorillaVelocityTracker>();
        left = GTPlayer.Instance.RightHand.controllerTransform.AddComponent<LocalGorillaVelocityTracker>();
        ReloadConfiguration();
        base.OnEnable();
    }


    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "-To fly, press 'Trigger' to Throw yourself,\n" +
               "both hands for more speed";
    }

    protected override void Cleanup()
    {
        Plugin.MenuController?.GetComponent<Fly>().button.RemoveBlocker(ButtonController.Blocker.MOD_INCOMPAT);
        if (right != null) right.Obliterate();
        if (left != null) left.Obliterate();
    }


    public static void BindConfigEntries()
    {
        Speed = Plugin.ConfigFile.Bind(
            DisplayName,
            "speed",
            5,
            "How fast you fly"
        );
    }
}