namespace Grate.Modules.Misc;

internal class DisableWind : GrateModule
{
    public static bool Enabled;

    protected override void Start()
    {
        Enabled = true;
        OnEnable();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Enabled = true;
    }

    public override string GetDisplayName()
    {
        return "Disable Wind";
    }

    public override string Tutorial()
    {
        return "Disables the wind barriers";
    }

    protected override void Cleanup()
    {
        Enabled = false;
    }
}