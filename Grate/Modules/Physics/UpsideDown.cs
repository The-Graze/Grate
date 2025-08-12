using Grate.GUI;
using Grate.Patches;
using UnityEngine;

namespace Grate.Modules.Physics;

public class UpsideDown : GrateModule
{
    private Vector3 baseGravity;
    
    protected override void Cleanup()
    {
        UpsideDownPatch.Enabled = false;
        UnityEngine.Physics.gravity = baseGravity;
        Plugin.MenuController?.GetComponent<LowGravity>().button.RemoveBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }

    protected override void Start()
    {
        base.Start();
        baseGravity = UnityEngine.Physics.gravity;
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built)
            return;
        
        base.OnEnable();

        UpsideDownPatch.Enabled = true;
        UnityEngine.Physics.gravity = -baseGravity;
        
        Plugin.MenuController?.GetComponent<LowGravity>().button.AddBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }

    public override string GetDisplayName() => "Upside Down";
    public override string Tutorial() => " - Puts you upside down.";
}