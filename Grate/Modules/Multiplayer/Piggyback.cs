using System;
using GorillaLocomotion;
using Grate.Gestures;
using Grate.GUI;
using Grate.Modules.Physics;
using Grate.Networking;
using Grate.Tools;
using UnityEngine;
using UnityEngine.XR;

namespace Grate.Modules.Multiplayer;

public class Piggyback : GrateModule
{
    private const float mountDistance = 1.5f;
    public static readonly string DisplayName = "Piggyback";
    public static bool mounted;

    public static Piggyback Instance;
    private readonly Vector3 mountOffset = new(0, 1f, -1f);
    private bool latchedWithLeft;
    private Transform mount;
    private VRRig mountedRig;
    private Vector3 mountPosition;

    private void Awake()
    {
        Instance = this;
    }

    protected override void Start()
    {
        base.Start();
    }

    private void FixedUpdate()
    {
        if (mounted)
        {
            if (RevokingConsent(mountedRig))
            {
                Unmount();
                GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(98, false, 1f);
            }
            else
            {
                var position = mount.TransformPoint(mountOffset);
                GTPlayer.Instance.TeleportTo(mount);
            }
        }
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        GestureTracker.Instance.leftGrip.OnPressed += Latch;
        GestureTracker.Instance.leftGrip.OnReleased += Unlatch;
        GestureTracker.Instance.rightGrip.OnPressed += Latch;
        GestureTracker.Instance.rightGrip.OnReleased += Unlatch;
    }

    private void Mount(Transform t, VRRig rig)
    {
        mountPosition = GTPlayer.Instance.bodyCollider.transform.position;
        mountedRig = rig;
        mounted = true;
        mount = t;
        EnableNoClip();
    }


    private void Unmount()
    {
        mount = null;
        mounted = false;
        mountedRig = null;
        mount = null;
        DisableNoClip();
        Invoke(nameof(WarpBack), .05f);
    }

    private void WarpBack()
    {
        GTPlayer.Instance.TeleportTo(mountPosition, GTPlayer.Instance.turnParent.transform.rotation);
    }

    public RigScanResult ClosestRig(Transform hand)
    {
        VRRig closestRig = null;
        Transform closestTransform = null;
        var closestDistance = Mathf.Infinity;
        foreach (var rig in GorillaParent.instance.vrrigs)
            try
            {
                if (rig.OwningNetPlayer.IsLocal) continue;
                var rigTransform = rig.transform.FindChildRecursive("head");
                var distanceToTarget = Vector3.Distance(hand.position, rigTransform.position);

                if (distanceToTarget < closestDistance)
                {
                    closestDistance = distanceToTarget;
                    closestTransform = rigTransform;
                    closestRig = rig;
                }
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }

        return new RigScanResult
        {
            transform = closestTransform,
            distance = closestDistance,
            rig = closestRig
        };
    }

    private bool GivingConsent(VRRig rig)
    {
        var np = rig.GetComponent<NetworkedPlayer>();
        if (Plugin.localPlayerTrusted) return true;
        return
            (np.RightTriggerPressed &&
             np.RightGripPressed &&
             np.RightThumbAmount < .25f &&
             Vector3.Dot(Vector3.up, rig.rightHandTransform.forward) > 0)
            ||
            (np.LeftTriggerPressed &&
             np.LeftGripPressed &&
             np.LeftThumbAmount < .25f &&
             Vector3.Dot(Vector3.up, rig.leftHandTransform.forward) > 0)
            ;
    }

    private bool RevokingConsent(VRRig rig)
    {
        var np = rig.GetComponent<NetworkedPlayer>();
        if (Plugin.localPlayerTrusted) return false;
        return
            (np.RightTriggerPressed &&
             np.RightGripPressed &&
             np.RightThumbAmount < .25f &&
             Vector3.Dot(Vector3.down, rig.rightHandTransform.forward) > 0)
            ||
            (np.LeftTriggerPressed &&
             np.LeftGripPressed &&
             np.LeftThumbAmount < .25f &&
             Vector3.Dot(Vector3.down, rig.leftHandTransform.forward) > 0)
            ;
    }

    private void EnableNoClip()
    {
        var noclip = Plugin.menuController.GetComponent<NoClip>();
        noclip.button.AddBlocker(ButtonController.Blocker.PIGGYBACKING);
        noclip.enabled = true;
    }

    private void DisableNoClip()
    {
        var noclip = Plugin.menuController.GetComponent<NoClip>();
        noclip.button.RemoveBlocker(ButtonController.Blocker.PIGGYBACKING);
        noclip.enabled = false;
    }

    private bool TryMount(bool isLeft)
    {
        var hand = isLeft ? GestureTracker.Instance.leftHand : GestureTracker.Instance.rightHand;
        var closest = ClosestRig(hand.transform);
        if (closest.distance < mountDistance && enabled && !mounted)
            if (GivingConsent(closest.rig))
            {
                if (!PositionValidator.Instance.isValidAndStable)
                {
                    GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(68, false, 1f);
                    return false;
                }

                Mount(closest.rig.headConstraint, closest.rig);
                return true;
            }

        return false;
    }

    private void Latch(InputTracker input)
    {
        if (input.node == XRNode.LeftHand)
            latchedWithLeft = TryMount(true);
        else
            latchedWithLeft = !TryMount(false);
    }

    private void Unlatch(InputTracker input)
    {
        if (!enabled || !mounted) return;
        if ((input.node == XRNode.LeftHand && latchedWithLeft) ||
            (input.node == XRNode.RightHand && !latchedWithLeft))
            Unmount();
    }

    protected override void Cleanup()
    {
        if (!MenuController.Instance.Built) return;
        if (mounted)
            Unmount();
        if (GestureTracker.Instance is null) return;
        GestureTracker.Instance.leftGrip.OnPressed -= Latch;
        GestureTracker.Instance.leftGrip.OnReleased -= Unlatch;
        GestureTracker.Instance.rightGrip.OnPressed -= Latch;
        GestureTracker.Instance.rightGrip.OnReleased -= Unlatch;
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "- To mount a player, ask them to give you a thumbs up.\n" +
               "- Hold down [Grip] near their head to hop on.\n" +
               "- If the player gives you a thumbs down you will be dismounted.";
    }

    public struct RigScanResult
    {
        public Transform transform;
        public VRRig rig;
        public float distance;
    }
}