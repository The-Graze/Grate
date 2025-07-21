using System;
using System.Collections;
using Fusion;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
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
        localWings = Plugin.assetBundle?.LoadAsset<GameObject>("ShadowWings");
        AddNet();
    }

    void AddNet()
    {
        try
        {
            NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
            VRRigCachePatches.OnRigCached += OnRigCached;
            base.Start();
        }
        catch (Exception e)
        {
            Application.Quit(1);
            Debug.LogException(e);
        }
        base.Start();
    }
    

    protected override void OnEnable()
    {
        localWings?.SetActive(true);
        GorillaTagger.Instance.offlineVRRig.AddComponent<NetShadWing>();
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
        if(arg1.Rig()?.GetComponent<NetShadWing>())
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
    
    class NetShadWing : MonoBehaviour
    {
        private GameObject? Wings;
        private VRRig Rig;

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