using Grate.GUI;
using Grate.Patches;

namespace Grate.Modules.Physics;

public class NoSlip : GrateModule
{
    public static readonly string DisplayName = "No Slip";
    public static NoSlip Instance;

    private void Awake()
    {
        Instance = this;
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        if (SlipperyHands.Instance)
            SlipperyHands.Instance.enabled = false;
    }

    protected override void Cleanup()
    {
        var s = $"The functionality for this module is in {nameof(SlidePatch)}";
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Effect: You no longer slide on slippery surfaces.";
    }
}