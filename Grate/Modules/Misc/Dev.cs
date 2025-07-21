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

public class Developer : GrateModule
{
    public static string DisplayName = "Dev Phone";
    private static GameObject Phone;

    protected override void Start()
    {
        base.Start();
        if (Phone == null)
        {
            Phone = Instantiate(Plugin.assetBundle.LoadAsset<GameObject>("DEVPHONE"));
            Phone.transform.SetParent(GestureTracker.Instance.rightHand.transform, true);
            Phone.transform.localPosition = new Vector3(-1.5f, 0.2f, 0.1f);
            Phone.transform.localRotation = Quaternion.Euler(2, 10, 0);
            Phone.transform.localScale /= 2;
        }

        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
        Phone.SetActive(false);
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            Phone.SetActive(true);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool enabled)
    {
        if (mod == DisplayName && PlayerExtensions.IsDev(player))
        {
            if (enabled)
                player.Rig().gameObject.GetOrAddComponent<NetDevPhone>();
            else
                Destroy(player.Rig().gameObject.GetComponent<NetDevPhone>());
        }
    }

    protected override void Cleanup()
    {
        Phone?.SetActive(false);
    }

    private void OnRigCached(NetPlayer player, VRRig rig)
    {
        rig?.gameObject?.GetComponent<NetDevPhone>()?.Obliterate();
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Given to the devs";
    }

    private class NetDevPhone : MonoBehaviour
    {
        private NetworkedPlayer networkedPlayer;
        private GameObject phone;

        private void OnEnable()
        {
            networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
            var rightHand = networkedPlayer.rig.rightHandTransform;

            phone = Instantiate(Phone);

            phone.transform.SetParent(rightHand);
            phone.transform.localPosition = new Vector3(0.0992f, 0.06f, 0.02f);
            phone.transform.localRotation = Quaternion.Euler(270, 163.12f, 0);
            phone.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            phone.SetActive(true);
        }

        private void OnDisable()
        {
            phone.Obliterate();
        }

        private void OnDestroy()
        {
            phone.Obliterate();
        }
    }
}