using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Gestures;
using Grate.GUI;
using UnityEngine;

namespace Grate.Modules.Movement;

public class DoubleJump : GrateModule
{
    public static readonly string DisplayName = "Double Jump";
    public static bool canDoubleJump = true;

    public static ConfigEntry<string> JumpForce;
    private GTPlayer _player;
    private Rigidbody _rigidbody;

    private Vector3 direction;
    public static bool primaryPressed => GestureTracker.Instance.rightPrimary.pressed;

    private void FixedUpdate()
    {
        if (_player.wasRightHandColliding || _player.wasLeftHandColliding) canDoubleJump = true;
        if (canDoubleJump && primaryPressed && !(_player.wasRightHandColliding || _player.wasLeftHandColliding))
        {
            direction = (_player.headCollider.transform.forward + Vector3.up) / 2;
            _rigidbody.velocity = new Vector3(direction.x, direction.y, direction.z) * _player.maxJumpSpeed *
                                  _player.scale * GetJumpForce(JumpForce.Value);
            canDoubleJump = false;
        }
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        _player = GTPlayer.Instance;
        _rigidbody = _player.bodyCollider.attachedRigidbody;
    }

    private float GetJumpForce(string jumpforce)
    {
        switch (jumpforce)
        {
            default:
                return 2;
            case "Normal":
                return 2;
            case "Medium":
                return 2.5f;
            case "High":
                return 2.8f;
            case "Super Jump":
                return 3.3f;
        }
    }

    public static void BindConfigEntries()
    {
        JumpForce = Plugin.configFile.Bind(
            DisplayName,
            "Jump Force",
            "Normal",
            new ConfigDescription(
                "How high you jump",
                new AcceptableValueList<string>("Normal", "Medium", "High", "Super Jump")
            )
        );
    }

    protected override void Cleanup()
    {
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Press [A / B] on your right controller to do a double jump in the air.";
    }
}