using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Modules.Movement;
using GorillaLocomotion;
using Grate.Networking;
using Grate.Tools;
using System;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using NetworkPlayer = NetPlayer;

namespace Grate.Modules.Misc
{
    public class RatSword : GrateModule
    {
        public static readonly string DisplayName = "Rat Sword";
        private GameObject sword;
        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();

            try
            {
                sword = Instantiate(Plugin.assetBundle.LoadAsset<GameObject>("Rat Sword"));
                sword.GetComponent<MeshRenderer>().materials[1].shader = sword.GetComponent<MeshRenderer>().materials[0].shader;
                sword.transform.SetParent(GestureTracker.Instance.rightHand.transform, true);
                sword.transform.localPosition = new Vector3(-0.4782f, 0.1f, 0.4f);
                sword.transform.localRotation = Quaternion.Euler(9, 0, 0);
                sword.transform.localScale /= 2;
                sword.SetActive(false);
                GestureTracker.Instance.rightGrip.OnPressed += ToggleRatSwordOn;
                GestureTracker.Instance.rightGrip.OnReleased += ToggleRatSwordOff;
                NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
                Patches.VRRigCachePatches.OnRigCached += OnRigCached;
            }
            catch (Exception e) { Logging.Exception(e); }
        }
        void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool enabled)
        {
            if (mod != DisplayName)
            { return; }
            if (enabled)
            {
                player.Rig().gameObject.GetOrAddComponent<NetSword>();
            }
            else
            {
                Destroy(player.Rig().gameObject.GetComponent<NetSword>());
            }
        }


        void ToggleRatSwordOn(InputTracker tracker)
        {
            sword?.SetActive(true);
        }

        void ToggleRatSwordOff(InputTracker tracker)
        {
            sword?.SetActive(false);
        }

        protected override void Cleanup()
        {
            GestureTracker.Instance.rightGrip.OnPressed -= ToggleRatSwordOn;
            GestureTracker.Instance.rightGrip.OnReleased -= ToggleRatSwordOff;
            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged -= OnPlayerModStatusChanged;
            sword?.Obliterate();
        }

        private void OnRigCached(Player player, VRRig rig)
        {
            rig?.gameObject?.GetComponent<NetSword>()?.Obliterate();
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "I met a lil' kid in canyons who wanted me to make him a sword.\n" +
                "[Grip] to wield your weapon, rat kid.";
        }
    }

    class NetSword : MonoBehaviour
    {
        NetworkedPlayer networkedPlayer;
        GameObject sword;

        void OnEnable()
        {
            networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
            var rightHand = networkedPlayer.rig.rightHandTransform;

            sword = Instantiate(Plugin.assetBundle.LoadAsset<GameObject>("Rat Sword"));
            sword.GetComponent<MeshRenderer>().materials[1].shader = sword.GetComponent<MeshRenderer>().materials[0].shader;

            sword.transform.SetParent(rightHand);
            sword.transform.localPosition = new Vector3(-0.4782f, 0.1f, 0.4f);
            sword.transform.localRotation = Quaternion.Euler(9, 0, 0);
            sword.transform.localScale /= 2;

            networkedPlayer.OnGripPressed += OnGripPressed;
            networkedPlayer.OnGripReleased += OnGripReleased;
        }

        void OnGripPressed(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft)
            {
                sword.SetActive(true);
            }
        }
        void OnGripReleased(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft)
            {
                sword.SetActive(false);
            }
        }

        void OnDestroy()
        {
            networkedPlayer.OnGripPressed -= OnGripPressed;
            networkedPlayer.OnGripReleased -= OnGripReleased;
            sword.Obliterate();
        }
    }
}
