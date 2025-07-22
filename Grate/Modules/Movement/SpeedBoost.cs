using System;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.GUI;
using Grate.Tools;

namespace Grate.Modules;

public class SpeedBoost : GrateModule
{
    public static readonly string DisplayName = "Speed Boost";
    public static float baseVelocityLimit, scale = 1.5f;
    public static bool active;

    public static ConfigEntry<int> Speed;

    private void FixedUpdate()
    {
        var progress = "";
        try
        {
            progress = "Getting Gamemode\n";
            var gameMode = GorillaGameManager.instance?.GameModeName();
            progress = "Checking status\n";
            if (active && (gameMode is null || gameMode == "NONE" || gameMode == "CASUAL"))
            {
                progress = "Setting multiplier\n";
                GTPlayer.Instance.jumpMultiplier = 1.3f * scale;
                GTPlayer.Instance.maxJumpSpeed = 8.5f * scale;
            }
        }
        catch (Exception e)
        {
            Logging.Debug("GorillaGameManager.instance is null:", GorillaGameManager.instance is null);
            Logging.Debug("GorillaGameManager.instance.GameMode() is null:",
                GorillaGameManager.instance?.GameModeName() is null);
            Logging.Debug(progress);
            Logging.Exception(e);
        }
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        active = true;
        baseVelocityLimit = GTPlayer.Instance.velocityLimit;
        ReloadConfiguration();
    }

    protected override void Cleanup()
    {
        if (active)
        {
            scale = 1;
            GTPlayer.Instance.velocityLimit = baseVelocityLimit;
            active = false;
        }
    }

    protected override void ReloadConfiguration()
    {
        scale = 1 + Speed.Value / 20f;
        if (enabled)
            GTPlayer.Instance.velocityLimit = baseVelocityLimit * scale;
    }

    public static void BindConfigEntries()
    {
        Speed = Plugin.ConfigFile.Bind(
            DisplayName,
            "speed",
            5,
            "How fast you run while speed boost is active"
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Effect: Increases your jump strength.";
    }
}