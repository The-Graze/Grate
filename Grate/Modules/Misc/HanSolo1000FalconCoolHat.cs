using System;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using Grate.Patches;
using Grate.Tools;
using UnityEngine;
using NetworkPlayer = NetPlayer;

namespace Grate.Modules.Misc;

public class HanSolo1000FalconCoolHat : GrateModule
{
    public static readonly string DisplayName = "HanSolo1000Falcons magical hat";
    private static GameObject HanSolo1000FalconHat;

    // y u changing this mah bro
    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        HanSolo1000FalconHat = Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>("goudabuda"));
        
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
        
        HanSolo1000FalconHat.transform.SetParent(GestureTracker.Instance.rightHand.transform, true);
        HanSolo1000FalconHat.transform.localPosition = new Vector3(-0.4782f, 0.1f, 0.4f);
        HanSolo1000FalconHat.transform.localRotation = Quaternion.Euler(9, 0, 0);
        HanSolo1000FalconHat.SetActive(false);
        
        try
        {
            GestureTracker.Instance.rightGrip.OnPressed += ToggleHanSolo1000FalconHatOn;
            GestureTracker.Instance.rightGrip.OnReleased += ToggleHanSolo1000FalconHatOff;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool enabled)
    {
        if (mod == DisplayName && player != NetworkSystem.Instance.LocalPlayer)
        {
            if (enabled)
                player.Rig().gameObject.GetOrAddComponent<NetHanSolo1000FalconHat>();
            else
                Destroy(player.Rig().gameObject.GetComponent<NetHanSolo1000FalconHat>());
        }
    }


    private void ToggleHanSolo1000FalconHatOn(InputTracker tracker) => HanSolo1000FalconHat?.SetActive(true);
    private void ToggleHanSolo1000FalconHatOff(InputTracker tracker) => HanSolo1000FalconHat?.SetActive(false);

    protected override void Cleanup()
    {
        HanSolo1000FalconHat?.Obliterate();
        if (GestureTracker.Instance != null)
        {
            GestureTracker.Instance.rightGrip.OnPressed -= ToggleHanSolo1000FalconHatOn;
            GestureTracker.Instance.rightGrip.OnReleased -= ToggleHanSolo1000FalconHatOff;
        }

        if (NetworkPropertyHandler.Instance != null)
            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged -= OnPlayerModStatusChanged;
    }

    private void OnRigCached(NetPlayer player, VRRig rig) => rig?.gameObject?.GetComponent<NetHanSolo1000FalconHat>()?.Obliterate();
    public override string GetDisplayName() => DisplayName;
    public override string Tutorial() => "- hansolo1000falcon can make a wind barrier with this (if you're not hansolo1000falcon, bad bad)";

    private class NetHanSolo1000FalconHat : MonoBehaviour
    {
        private GameObject HanSolo1000FalconHatNet;
        private NetworkedPlayer networkedPlayer;

        private void OnEnable()
        {
            networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
            var rightHand = networkedPlayer.rig.rightHandTransform;

            HanSolo1000FalconHatNet = Instantiate(HanSolo1000FalconHat);

            HanSolo1000FalconHatNet.transform.SetParent(rightHand);
            HanSolo1000FalconHatNet.transform.localPosition = new Vector3(0.04f, 0.05f, -0.02f);
            HanSolo1000FalconHatNet.transform.localRotation = Quaternion.Euler(78.4409f, 0, 0);
            HanSolo1000FalconHatNet.transform.localScale = new Vector3(1f, 1f, 1f);

            networkedPlayer.OnGripPressed += OnGripPressed;
            networkedPlayer.OnGripReleased += OnGripReleased;

            if (networkedPlayer.owner.UserId != "A48744B93D9A3596")
                HanSolo1000FalconHatNet.Obliterate();
        }

        private void OnDestroy()
        {
            networkedPlayer.OnGripPressed -= OnGripPressed;
            networkedPlayer.OnGripReleased -= OnGripReleased;
            HanSolo1000FalconHatNet.Obliterate();
        }

        private void OnGripPressed(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft)
                HanSolo1000FalconHatNet.SetActive(true);
        }

        private void OnGripReleased(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft)
                HanSolo1000FalconHatNet.SetActive(false);
        }
    }
}