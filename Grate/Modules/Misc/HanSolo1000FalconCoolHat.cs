using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using Grate.Patches;
using UnityEngine;

namespace Grate.Modules.Misc
{
    public class HanSolo1000FalconCoolHat : GrateModule
    {
        private static GameObject Hat;
        
        protected override void Start()
        {
            base.Start();
            
            if (Hat == null)
            {
                Hat = Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>("goudabuda"), GestureTracker.Instance.rightHand.transform);
                Hat.transform.localPosition = new Vector3(0.0992f, 0.06f, 0.02f);
                Hat.transform.localRotation = Quaternion.Euler(270, 163.12f, 0);
                Hat.transform.localScale *= 20f;
            }
            
            Hat.SetActive(false);
            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
            VRRigCachePatches.OnRigCached += OnRigCached;
        }
        
        private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool enabled)
        {
            if (mod == GetDisplayName() && player != NetworkSystem.Instance.LocalPlayer && player.UserId == "A48744B93D9A3596")
            {
                if (enabled)
                    player.Rig().gameObject.GetOrAddComponent<NetHanSolo1000FalconCoolHat>();
                else
                    Destroy(player.Rig().gameObject.GetComponent<NetHanSolo1000FalconCoolHat>());
            }
        }
        
        protected override void OnEnable()
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            Hat.SetActive(true);
        }

        protected override void Cleanup() => Hat.SetActive(false);
        private void OnRigCached(NetPlayer player, VRRig rig) => rig?.gameObject?.GetComponent<NetHanSolo1000FalconCoolHat>()?.Obliterate();
        public override string GetDisplayName() => "HanSolo1000Falcons Cool Hat";
        public override string Tutorial() => " -HanSolo1000Falcon gets a hella cool hat";

        private class NetHanSolo1000FalconCoolHat : MonoBehaviour
        {
            private GameObject netHat;
            private NetworkedPlayer networkedPlayer;

            private void OnEnable()
            {
                networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
                Transform rightHand = networkedPlayer.rig.rightHandTransform;

                netHat = Instantiate(Hat, rightHand);
                netHat.transform.localPosition = new Vector3(0.0992f, 0.06f, 0.02f);
                netHat.transform.localRotation = Quaternion.Euler(270, 163.12f, 0);

                netHat.SetActive(true);
            }

            private void OnDisable() => netHat.Obliterate();
            private void OnDestroy() => netHat.Obliterate();
        }
    }
}