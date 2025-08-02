using System;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using Grate.Patches;
using Grate.Tools;
using UnityEngine;
using NetworkPlayer = NetPlayer;

namespace Grate.Modules.Misc
{
    public class HanSolo1000FalconCoolHat : GrateModule
    {
        public static readonly string DisplayName = "HanSolo1000Falcons magical hat";

        private const string HatAssetName = "goudabuda";
        private const string HanSolo1000FalconsUserIdHeIsSoCool = "A48744B93D9A3596";

        private static GameObject localHat;

        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built)
                return;

            base.OnEnable();

            localHat = Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>(HatAssetName));
            localHat.transform.SetParent(GestureTracker.Instance.rightHand.transform, true);
            localHat.transform.localPosition = new Vector3(-0.4782f, 0.1f, 0.4f);
            localHat.transform.localRotation = Quaternion.Euler(9f, 0f, 0f);
            localHat.SetActive(false);
            
            try
            {
                GestureTracker.Instance.rightGrip.OnPressed += ToggleHatOn;
                GestureTracker.Instance.rightGrip.OnReleased += ToggleHatOff;
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }

            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
            VRRigCachePatches.OnRigCached += OnRigCached;
        }

        private void ToggleHatOn(InputTracker tracker) => localHat?.SetActive(true);
        private void ToggleHatOff(InputTracker tracker) => localHat?.SetActive(false);

        private void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool enabled)
        {
            if (mod != DisplayName || player == NetworkSystem.Instance.LocalPlayer)
                return;

            if (enabled)
                player.Rig().gameObject.GetOrAddComponent<NetHanSolo1000FalconHat>();
            else
                Destroy(player.Rig().gameObject.GetComponent<NetHanSolo1000FalconHat>());
        }

        private void OnRigCached(NetPlayer player, VRRig rig)
        {
            rig?.gameObject?.GetComponent<NetHanSolo1000FalconHat>()?.Obliterate();
        }

        protected override void Cleanup()
        {
            localHat?.Obliterate();
            localHat = null;

            if (GestureTracker.Instance != null)
            {
                GestureTracker.Instance.rightGrip.OnPressed -= ToggleHatOn;
                GestureTracker.Instance.rightGrip.OnReleased -= ToggleHatOff;
            }

            if (NetworkPropertyHandler.Instance != null)
                NetworkPropertyHandler.Instance.OnPlayerModStatusChanged -= OnPlayerModStatusChanged;

            VRRigCachePatches.OnRigCached -= OnRigCached;
        }

        public override string GetDisplayName() => DisplayName;

        public override string Tutorial() =>
            "- HanSolo1000Falcon can make a wind barrier with this hat. If you're not HanSolo1000Falcon... no hat for you.";

        private class NetHanSolo1000FalconHat : MonoBehaviour
        {
            private GameObject hatInstance;
            private NetworkedPlayer networkedPlayer;

            private void OnEnable()
            {
                networkedPlayer = GetComponent<NetworkedPlayer>();
                var rightHand = networkedPlayer.rig?.rightHandTransform;

                hatInstance = Instantiate(localHat);
                hatInstance.transform.SetParent(rightHand);
                hatInstance.transform.localPosition = new Vector3(0.04f, 0.05f, -0.02f);
                hatInstance.transform.localRotation = Quaternion.Euler(78.4409f, 0f, 0f);
                hatInstance.transform.localScale = Vector3.one;

                networkedPlayer.OnGripPressed += HandleGripPressed;
                networkedPlayer.OnGripReleased += HandleGripReleased;

                if (networkedPlayer.owner.UserId != HanSolo1000FalconsUserIdHeIsSoCool)
                    hatInstance.Obliterate();
            }

            private void OnDestroy()
            {
                networkedPlayer.OnGripPressed -= HandleGripPressed;
                networkedPlayer.OnGripReleased -= HandleGripReleased;
                hatInstance?.Obliterate();
            }

            private void HandleGripPressed(NetworkedPlayer player, bool isLeft)
            {
                if (!isLeft)
                    hatInstance?.SetActive(true);
            }

            private void HandleGripReleased(NetworkedPlayer player, bool isLeft)
            {
                if (!isLeft)
                    hatInstance?.SetActive(false);
            }
        }
    }
}