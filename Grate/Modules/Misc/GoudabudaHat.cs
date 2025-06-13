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

public class GoudabudaHat : GrateModule
{
    public static readonly string DisplayName = "Goudabuda's magical hat";
    private static GameObject goudabudaHat;

    // y u changing this mah bro
    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        goudabudaHat = Instantiate(Plugin.assetBundle.LoadAsset<GameObject>("goudabuda"));
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
        goudabudaHat.transform.SetParent(GestureTracker.Instance.rightHand.transform, true);
        goudabudaHat.transform.localPosition = new Vector3(-0.4782f, 0.1f, 0.4f);
        goudabudaHat.transform.localRotation = Quaternion.Euler(9, 0, 0);
        goudabudaHat.transform.localScale /= 4;
        goudabudaHat.SetActive(false);
        try
        {
            GestureTracker.Instance.rightGrip.OnPressed += ToggleGoudabudaHatOff;
            GestureTracker.Instance.rightGrip.OnReleased += ToggleGoudabudaHatOff;
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
                player.Rig().gameObject.GetOrAddComponent<NetGoudabudaHat>();
            else
                Destroy(player.Rig().gameObject.GetComponent<NetGoudabudaHat>());
        }
    }


    private void ToggleGoudabudaHatOn(InputTracker tracker)
    {
        goudabudaHat?.SetActive(true);
    }

    private void ToggleGoudabudaHatOff(InputTracker tracker)
    {
        goudabudaHat?.SetActive(false);
    }

    protected override void Cleanup()
    {
        goudabudaHat?.Obliterate();
        if (GestureTracker.Instance != null)
        {
            GestureTracker.Instance.rightGrip.OnPressed -= ToggleGoudabudaHatOn;
            GestureTracker.Instance.rightGrip.OnReleased -= ToggleGoudabudaHatOff;
        }

        if (NetworkPropertyHandler.Instance != null)
            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged -= OnPlayerModStatusChanged;
    }

    private void OnRigCached(NetPlayer player, VRRig rig)
    {
        rig?.gameObject?.GetComponent<NetGoudabudaHat>()?.Obliterate();
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "- goudabuda can make a wind barrier with this (if you're not goudabuda, fuck you)";
    }

    private class NetGoudabudaHat : MonoBehaviour
    {
        private GameObject goudabudaHat;
        private NetworkedPlayer networkedPlayer;

        private void OnEnable()
        {
            networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
            var rightHand = networkedPlayer.rig.rightHandTransform;

            goudabudaHat = Instantiate(goudabudaHat);

            goudabudaHat.transform.SetParent(rightHand);
            goudabudaHat.transform.localPosition = new Vector3(0.04f, 0.05f, -0.02f);
            goudabudaHat.transform.localRotation = Quaternion.Euler(78.4409f, 0, 0);
            goudabudaHat.transform.localScale = new Vector3(1f, 1f, 1f);

            networkedPlayer.OnGripPressed += OnGripPressed;
            networkedPlayer.OnGripReleased += OnGripReleased;

            if (networkedPlayer.owner.UserId != "A48744B93D9A3596") goudabudaHat.Obliterate();
        }

        private void OnDestroy()
        {
            networkedPlayer.OnGripPressed -= OnGripPressed;
            networkedPlayer.OnGripReleased -= OnGripReleased;
            goudabudaHat.Obliterate();
        }

        private void OnGripPressed(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft) goudabudaHat.SetActive(true);
        }

        private void OnGripReleased(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft) goudabudaHat.SetActive(false);
        }
    }
}