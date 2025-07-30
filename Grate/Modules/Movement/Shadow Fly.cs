using System;
using Grate.Extensions;
using Grate.Networking;
using Grate.Patches;
using UnityEngine;
using NetworkPlayer = NetPlayer;

namespace Grate.Modules.Movement;

public class ShadowFly : GrateModule
{
    private static GameObject? localWings;
    public static string DisplayName = "Shadow Fly";

    protected override void Start()
    {
        localWings = Plugin.AssetBundle?.LoadAsset<GameObject>("ShadowWings");
        AddNet();
    }


    protected override void OnEnable()
    {
        localWings?.SetActive(true);
        GorillaTagger.Instance.offlineVRRig.AddComponent<NetShadWing>();
    }

    private void AddNet()
    {
        try
        {
            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
            VRRigCachePatches.OnRigCached += OnRigCached;
            base.Start();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        base.Start();
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Fly With wings";
    }

    protected override void Cleanup()
    {
        GorillaTagger.Instance.offlineVRRig.GetComponent<NetShadWing>().Obliterate();
    }

    private void OnRigCached(NetPlayer arg1, VRRig arg2)
    {
        if (arg1.Rig()?.GetComponent<NetShadWing>())
            arg1.Rig()?.GetComponent<NetShadWing>().Obliterate();
    }

    private void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool modEnabled)
    {
        if (mod != GetDisplayName() /*|| player.UserId != "AE10C04744CCF6E7"*/) return;

        if (modEnabled)
            player.Rig().AddComponent<NetShadWing>();

        else
            player.Rig()?.GetComponent<NetShadWing>().Obliterate();
    }

    private class NetShadWing : MonoBehaviour
    {
        private VRRig Rig;
        private GameObject? Wings;

        private void FixedUpdate()
        {
            if (!Rig)
                Rig = GetComponent<VRRig>();

            if (!Wings)
                Wings = Instantiate(localWings, Rig.transform);
        }

        private void OnDestroy()
        {
            Wings?.Obliterate();
        }
    }
}