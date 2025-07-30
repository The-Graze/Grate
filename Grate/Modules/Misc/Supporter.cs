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

public class Supporter : GrateModule
{
    private static readonly string DisplayName = "Trusted Phone";
    private static GameObject? _phone;

    protected override void Start()
    {
        base.Start();
        if (_phone == null)
        {
            _phone = Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>("PHONE"));
            _phone.transform.SetParent(GestureTracker.Instance.rightHand.transform, true);
            _phone.transform.localPosition = new Vector3(-1.5f, 0.2f, 0.1f);
            _phone.transform.localRotation = Quaternion.Euler(2, 10, 0);
            _phone.transform.localScale /= 2;
        }

        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
        _phone?.SetActive(false);
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            _phone?.SetActive(true);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool modEnabled)
    {
        if (mod != DisplayName || !player.IsSupporter()) return;
        if (modEnabled)
            player.Rig()?.gameObject.GetOrAddComponent<NetPhone>();
        else
            Destroy(player.Rig()?.gameObject.GetComponent<NetPhone>());
    }

    protected override void Cleanup()
    {
        _phone?.SetActive(false);
    }

    private static void OnRigCached(NetworkPlayer player, VRRig rig)
    {
        rig?.gameObject?.GetComponent<NetPhone>()?.Obliterate();
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "given out to people the grate developers and the Supporters";
    }

    private class NetPhone : MonoBehaviour
    {
        private NetworkedPlayer? networkedPlayer;
        private GameObject? phone;

        private void OnEnable()
        {
            networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
            var rightHand = networkedPlayer.rig.rightHandTransform;

            phone = Instantiate(_phone, rightHand, false);

            if (phone == null)
                return;
            phone.transform.localPosition = new Vector3(0.0992f, 0.06f, 0.02f);
            phone.transform.localRotation = Quaternion.Euler(270, 163.12f, 0);
            phone.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            phone.SetActive(true);
        }

        private void OnDisable()
        {
            phone?.Obliterate();
        }

        private void OnDestroy()
        {
            phone?.Obliterate();
        }
    }
}