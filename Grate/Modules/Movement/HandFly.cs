using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Gestures;
using Grate.GUI;
using UnityEngine;

namespace Grate.Modules.Movement;

public class HandFly : GrateModule
{
    public static readonly string DisplayName = "Hand Fly";

    public static ConfigEntry<int> Speed;
    public static ConfigEntry<bool> LeftHanded;
    private readonly float acceleration = .1f;
    private float speedScale = 10f;

    private void FixedUpdate()
    {
        var tracker = GestureTracker.Instance;
        if (tracker.leftGrip.pressed && tracker.rightGrip.pressed && enabled)
        {
            // nullify gravity by adding it's negative value to the player's velocity
            var rb = GTPlayer.Instance.bodyCollider.attachedRigidbody;
            if (enabledModules.ContainsKey(Bubble.DisplayName)
                && !enabledModules[Bubble.DisplayName])
                rb.AddForce(-UnityEngine.Physics.gravity * rb.mass * GTPlayer.Instance.scale);

            var velocity = LeftHanded.Value ?
                ControllerInputPoller.instance.leftControllerPosition - ControllerInputPoller.instance.rightControllerPosition :
                ControllerInputPoller.instance.rightControllerPosition - ControllerInputPoller.instance.leftControllerPosition;
            velocity *= GTPlayer.Instance.scale * speedScale;
            rb.velocity = Vector3.Lerp(rb.velocity, velocity, acceleration);
        }
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        ReloadConfiguration();
    }

    protected override void ReloadConfiguration()
    {
        speedScale = Speed.Value * 2;
    }

    public static void BindConfigEntries()
    {
        Speed = Plugin.configFile.Bind(
            DisplayName,
            "speed",
            5,
            "How fast you fly"
        );

        LeftHanded = Plugin.configFile.Bind(
            DisplayName,
            "Left Handed Mode",
            false,
            "Are you left handed"
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "- To fly, press both grips. \n" +
               "- To fly around, move your hands to make an arrow. \n" +
               "- The further apart your hand.";
    }

    protected override void Cleanup()
    {
    }
}