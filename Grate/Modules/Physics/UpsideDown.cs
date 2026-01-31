using GorillaLocomotion;
using Grate.Extensions;
using UnityEngine;
using UnityEngine;
using Grate.GUI;
using Grate.Networking;
using Grate.Patches;

namespace Grate.Modules.Physics;

public class UpsideDown : GrateModule
{
    private Vector3 baseGravity;
    private Quaternion baseRotation;

    private Transform turnParent;


    protected override void Cleanup()
    {
        UpsideDownPatch.AffectedRigs.Remove(VRRig.LocalRig);
        UnityEngine.Physics.gravity = baseGravity;

        turnParent.rotation = baseRotation;

        Plugin.MenuController?.GetComponent<LowGravity>().button.RemoveBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }

    private void Awake()
    {
        baseGravity = UnityEngine.Physics.gravity;
        turnParent = GTPlayer.Instance.turnParent.transform;
    }

    protected override void Start()
    {
        base.Start();

        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
    }

    private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool enabled)
    {
        if (mod != GetDisplayName() || player == NetworkSystem.Instance.LocalPlayer) return;

        if (enabled)
            UpsideDownPatch.AffectedRigs.Add(player.Rig()!, false);
        else if (UpsideDownPatch.AffectedRigs.ContainsKey(player.Rig()!))
            UpsideDownPatch.AffectedRigs.Remove(player.Rig()!);
    }

    private static void OnRigCached(NetPlayer player, VRRig rig)
    {
        UpsideDownPatch.AffectedRigs.Remove(rig);
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance!.Built)
            return;

        base.OnEnable();

        UpsideDownPatch.AffectedRigs.Add(VRRig.LocalRig, false);
        UnityEngine.Physics.gravity = -baseGravity;

        baseRotation = turnParent.rotation;

        var oldRot = turnParent.rotation;
        oldRot.x = 180f;
        turnParent.rotation = oldRot;

        Plugin.MenuController?.GetComponent<LowGravity>().button.AddBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }

    public override string GetDisplayName()
    {
        return "Upside Down";
    }

    public override string Tutorial()
    {
        return " - Puts you upside down.\n - There is no safeguard! If you fall out of the map, you are done for!";
    }
}