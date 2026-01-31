using UnityEngine;
using UnityEngine;
﻿using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Gestures;
using Grate.GUI;

namespace Grate.Modules.Movement;

public class Helicopter : GrateModule
{
    public static readonly string DisplayName = "Helicopter";

    public static ConfigEntry<int> Speed;
    public static ConfigEntry<string> Mode;
    public static ConfigEntry<string> spin;

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        GestureTracker.Instance.OnGlide += OnGlide;
        Plugin.MenuController.GetComponent<Airplane>().button.AddBlocker(ButtonController.Blocker.MOD_INCOMPAT);
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
        var up = player.headCollider.transform.forward.y;
        if (player.LeftHand.wasColliding || player.rightHand.wasColliding) return;

        if (Threshold(15f, up))
        {
            var rigidbody = player.bodyCollider.attachedRigidbody;
            rigidbody.velocity = new Vector3(0, Speed.Value * GTPlayer.Instance.scale * Towards(up), 0);
            player.Turn(Speed.Value * Time.fixedDeltaTime * 20 * Towards(up) * (spin.Value == "normal" ? 1 : -1));
        }
    }

    public static bool Threshold(float angle, float direction)
    {
        if (direction > angle || direction < -angle) return false;

        return true;
    }

    public static float Towards(float direction)
    {
        if (Mode.Value == "snappy") return direction < 0 ? -1 : 1;

        return direction * 2;
    }

    protected override void Cleanup()
    {
        if (!MenuController.Instance.Built) return;
        if (!GestureTracker.Instance) return;
        GestureTracker.Instance.OnGlide -= OnGlide;
        Plugin.MenuController.GetComponent<Airplane>().button.RemoveBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }

    public static void BindConfigEntries()
    {
        Speed = Plugin.ConfigFile.Bind(
            DisplayName,
            "speed",
            5,
            "How fast you spin"
        );

        Mode = Plugin.ConfigFile.Bind(
            DisplayName,
            "mode",
            "snappy",
            new ConfigDescription(
                "The way your head controls the speed",
                new AcceptableValueList<string>("snappy", "smooth")
            )
        );

        spin = Plugin.ConfigFile.Bind(
            DisplayName,
            "direction",
            "normal",
            new ConfigDescription(
                "The direction of the spin",
                new AcceptableValueList<string>("normal", "reverse")
            )
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "- WARNING: CAN CAUSE MOTION SICKNESS EASILY \n" +
               " -To spin, do a T-pose (spread your arms out like wings on a Helicopter). \n" +
               "- Look up to fly up.\n" +
               "- Look down to fly down.";
    }
}