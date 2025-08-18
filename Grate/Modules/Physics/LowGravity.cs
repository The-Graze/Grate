using BepInEx.Configuration;
using Grate.GUI;
using UnityEngine;

namespace Grate.Modules.Physics;

public class LowGravity : GrateModule
{
    public static readonly string DisplayName = "Gravity";
    public static LowGravity Instance;

    public static ConfigEntry<int> Multiplier;
    public float gravityScale = .25f;
    private Vector3 baseGravity;
    public bool active { get; private set; }

    private void Awake()
    {
        Instance = this;
        baseGravity = UnityEngine.Physics.gravity;
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        ReloadConfiguration();
        Plugin.MenuController?.GetComponent<UpsideDown>().button.AddBlocker(ButtonController.Blocker.MOD_INCOMPAT);
        active = true;
    }

    protected override void Cleanup()
    {
        if (!active) return;
        UnityEngine.Physics.gravity = baseGravity;
        Plugin.MenuController?.GetComponent<UpsideDown>().button.RemoveBlocker(ButtonController.Blocker.MOD_INCOMPAT);
        active = false;
    }

    protected override void ReloadConfiguration()
    {
        gravityScale = Multiplier.Value / 5f;
        gravityScale = Mathf.Pow(gravityScale, 2f);
        UnityEngine.Physics.gravity = baseGravity * gravityScale;
    }

    public static void BindConfigEntries()
    {
        Multiplier = Plugin.ConfigFile.Bind(
            DisplayName,
            "multiplier",
            2,
            "How strong gravity will be (0=No gravity, 5=Normal gravity, 10=2x Jupiter Gravity)"
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Effect: Changes the strength of gravity. \n\nYou can modify the strength in the settings menu.";
    }
}