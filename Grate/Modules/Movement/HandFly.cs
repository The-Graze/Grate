using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.GUI;
using UnityEngine;

namespace Grate.Modules.Movement;

public class LocalGorillaVelocityTracker : MonoBehaviour
{
    private Vector3 previousLocalPosition;
    private Vector3 velocity;

    private void Start() { previousLocalPosition = transform.localPosition; }

    private void Update()
    {
        Vector3 localDisplacement = transform.localPosition - previousLocalPosition;
        Vector3 localVelocity = localDisplacement / Time.deltaTime;

        velocity = transform.parent.TransformDirection(localVelocity);

        previousLocalPosition = transform.localPosition;
    }

    public Vector3 GetVelocity() => velocity;
}

public class HandFly : GrateModule
{
    public static string DisplayName = "Hand Fly";
    private LocalGorillaVelocityTracker? right;
    private LocalGorillaVelocityTracker? left;

    private static ConfigEntry<int>? Speed;
    private float SpeedScale => Speed!.Value * 2.5f;

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built || !enabled) return;
        Plugin.menuController?.GetComponent<Fly>().button.AddBlocker(ButtonController.Blocker.MOD_INCOMPAT);
        right = GTPlayer.Instance.leftControllerTransform.AddComponent<LocalGorillaVelocityTracker>();
        left = GTPlayer.Instance.rightControllerTransform.AddComponent<LocalGorillaVelocityTracker>();
        ReloadConfiguration();
        base.OnEnable();
    }

    private void FixedUpdate()
    {
        if (ControllerInputPoller.instance.leftControllerIndexFloat > 0.5f)
            GorillaTagger.Instance.rigidbody.velocity -= right!.GetVelocity() / SpeedScale * GTPlayer.Instance.scale;

        if (ControllerInputPoller.instance.rightControllerIndexFloat > 0.5f)
            GorillaTagger.Instance.rigidbody.velocity -= left!.GetVelocity() / SpeedScale * GTPlayer.Instance.scale;
    }


    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "-To fly, press Grip to Throw yourself,\n" +
               "both hands for more speed";
    }

    protected override void Cleanup()
    {
        Plugin.menuController?.GetComponent<Fly>().button.RemoveBlocker(ButtonController.Blocker.MOD_INCOMPAT);
        if (right != null) right.Obliterate();
        if (left != null) left.Obliterate();
    }
    

    public static void BindConfigEntries()
    {
        Speed = Plugin.configFile.Bind(
            DisplayName,
            "speed",
            5,
            "How fast you fly"
        );
    }
}