using Grate.Modules;
using GorillaLocomotion;
using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

namespace Grate.Extensions
{
    public static class PlayerExtensions
    {
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
            //return rig.photonView;
            return Traverse.Create(rig).Field("photonView").GetValue<PhotonView>();
        }

        public static bool HasProperty(this VRRig rig, string key)
        {
            if(rig.OwningNetPlayer is NetPlayer player)
                return player.HasProperty(key);
            return false;
        }

        public static bool ModuleEnabled(this VRRig rig, string mod)
        {
            if(rig?.OwningNetPlayer is NetPlayer player)
                return player.ModuleEnabled(mod);
            return false;
        }

        public static T GetProperty<T>(this NetPlayer player, string key)
        {
            return (T)player?.GetPlayerRef().CustomProperties[key];
        }

        public static bool HasProperty(this NetPlayer player, string key)
        {
            return !(player?.GetPlayerRef().CustomProperties[key] is null);
        }

        public static bool ModuleEnabled(this NetPlayer player, string mod)
        {
            if (!player.HasProperty(GrateModule.enabledModulesKey)) return false;
            Dictionary<string, bool> enabledMods = player.GetProperty<Dictionary<string, bool>>(GrateModule.enabledModulesKey);
            if (enabledMods is null || !enabledMods.ContainsKey(mod)) return false;
            return enabledMods[mod];
        }

        public static VRRig Rig(this NetPlayer player)
        {
            foreach (var rig in GorillaParent.instance.vrrigs)
            {
                if (rig.OwningNetPlayer == player)
                    return rig;
            }
            return null;
        }
        public static bool IsDev(NetPlayer player)
        {
            HashSet<string> devs = new HashSet<string> {
                "42D7D32651E93866", //graze
                "9ABD0C174289F58E", //baggZ
                "B1B20DEEEDB71C63", //monky
                "A48744B93D9A3596"  //Goudabuda
            };
            return devs.Contains(player.UserId);
        }
        public static bool IsTrusted(NetPlayer player)
        {
            HashSet<string> trusted = new HashSet<string> {
                "D322FC7F6A9875DB" //DecalFree
            };
            return trusted.Contains(player.UserId) || IsDev(player);
        }
    }
}
