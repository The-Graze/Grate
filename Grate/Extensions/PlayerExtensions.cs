using System.Collections.Generic;
using System.Linq;
using GorillaLocomotion;
using Grate.Modules;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace Grate.Extensions;

public static class PlayerExtensions
{
    private static readonly HashSet<string> DeveloperIds = new()
    {
        "42D7D32651E93866", // graze
        "9ABD0C174289F58E", // baggZ
        "B1B20DEEEDB71C63", // monky
        "A48744B93D9A3596" // Goudabuda
    };

    public static void AddForce(this GTPlayer self, Vector3 v)
    {
        self.GetComponent<Rigidbody>().velocity += v;
    }

    public static void SetVelocity(this GTPlayer self, Vector3 v)
    {
        self.GetComponent<Rigidbody>().velocity = v;
    }

    public static PhotonView PhotonView(this VRRig rig)
    {
        // Access private photonView via reflection
        return Traverse.Create(rig).Field("photonView").GetValue<PhotonView>();
    }

    public static bool HasProperty(this VRRig rig, string key)
    {
        return rig?.OwningNetPlayer?.HasProperty(key) ?? false;
    }

    public static bool ModuleEnabled(this VRRig rig, string mod)
    {
        return rig?.OwningNetPlayer?.ModuleEnabled(mod) ?? false;
    }

    public static T GetProperty<T>(this NetPlayer? player, string key)
    {
        return (T)player?.GetPlayerRef().CustomProperties[key];
    }

    public static bool HasProperty(this NetPlayer player, string key)
    {
        return player?.GetPlayerRef().CustomProperties.ContainsKey(key) ?? false;
    }

    public static bool ModuleEnabled(this NetPlayer player, string mod)
    {
        if (!player.HasProperty(GrateModule.enabledModulesKey)) return false;

        var enabledMods = player.GetProperty<Dictionary<string, bool>>(GrateModule.enabledModulesKey);
        return enabledMods.TryGetValue(mod, out var enabled) && enabled;
    }

    public static VRRig? Rig(this NetPlayer? player)
    {
        return GorillaParent.instance.vrrigs.FirstOrDefault(rig => rig.OwningNetPlayer == player);
    }

    // Use Plugin.localPlayerDev for checking if the local player is a dev
    public static bool IsDev(this NetPlayer player)
    {
        return DeveloperIds.Contains(player.UserId);
    }

    // Use Plugin.localPlayerTrusted for checking if the local player is trusted
    public static bool IsAdmin(this NetPlayer player)
    {
        return false;
    }

    public static bool IsSupporter(this NetPlayer player)
    {
        return false;
    }
}