using GorillaLocomotion;
using Grate.Extensions;
using Grate.GUI;
using Grate.Networking;
using Grate.Patches;
using UnityEngine;

namespace Grate.Modules.Physics;

public class UpsideDown : GrateModule
{
    private Vector3 baseGravity;

    private Transform turnParent;
    
    protected override void Cleanup()
    {
        UpsideDownPatch.AffectedRigs.Remove(VRRig.LocalRig);
        UnityEngine.Physics.gravity = baseGravity;
        
        Quaternion oldRot = turnParent.rotation;
        oldRot.x = 0f;
        turnParent.rotation = oldRot;
        
        Plugin.MenuController?.GetComponent<LowGravity>().button.RemoveBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }

    protected override void Start()
    {
        base.Start();
        
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
    }
    
    private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool enabled)
    {
        if (mod == GetDisplayName() && player != NetworkSystem.Instance.LocalPlayer)
        {
            if (enabled)
                UpsideDownPatch.AffectedRigs.Add(player.Rig());
            else if (UpsideDownPatch.AffectedRigs.Contains(player.Rig()))
                UpsideDownPatch.AffectedRigs.Remove(player.Rig());
        }
    }

    private void OnRigCached(NetPlayer player, VRRig rig)
    {
        if (UpsideDownPatch.AffectedRigs.Contains(rig))
            UpsideDownPatch.AffectedRigs.Remove(rig);
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built)
            return;
        
        base.OnEnable();

        UpsideDownPatch.AffectedRigs.Add(VRRig.LocalRig);
        UnityEngine.Physics.gravity = -baseGravity;
        
        Quaternion oldRot = turnParent.rotation;
        oldRot.x = 180f;
        turnParent.rotation = oldRot;
        
        Plugin.MenuController?.GetComponent<LowGravity>().button.AddBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }
    
    private void Awake()
    {
        baseGravity = UnityEngine.Physics.gravity;
        turnParent = GTPlayer.Instance.turnParent.transform;
    }

    public override string GetDisplayName() => "Upside Down";
    public override string Tutorial() => " - Puts you upside down.\n - There is no safeguard! If you fall out of the map, you are done for!";
}