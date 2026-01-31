using Grate.Extensions;
using UnityEngine;
using UnityEngine;
﻿using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using Grate.Patches;
using Photon.Pun;
using UnityEngine.XR;

namespace Grate.Modules.Misc;

internal class StoneBroke : GrateModule
{
    public static GameObject wawa;

    public static InputTracker? inputL;
    public static InputTracker? inputR;
    private Awsomepnix LocalP;

    private void Awake()
    {
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
    }

    protected override void Start()
    {
        base.Start();
        wawa = Plugin.AssetBundle.LoadAsset<GameObject>("bs");
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        LocalP = GorillaTagger.Instance.offlineVRRig.AddComponent<Awsomepnix>();
    }

    public override string GetDisplayName()
    {
        return "StoneBroke :3";
    }

    public override string Tutorial()
    {
        return "MuskEnjoyer";
    }

    private void OnRigCached(NetPlayer player, VRRig rig)
    {
        if (rig?.gameObject?.GetComponent<Awsomepnix>() != null)
        {
            rig?.gameObject?.GetComponent<Awsomepnix>()?.ps.Obliterate();
            rig?.gameObject?.GetComponent<Awsomepnix>()?.Obliterate();
        }
    }


    private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool enabled)
    {
        if (mod == GetDisplayName() && player.UserId == "CA8FDFF42B7A1836")
        {
            if (enabled)
            {
                player.Rig().gameObject.GetOrAddComponent<Awsomepnix>();
            }
            else
            {
                player.Rig().gameObject.GetComponent<Awsomepnix>().ps.gameObject.Obliterate();
                player.Rig().gameObject.GetComponent<Awsomepnix>().Obliterate();
            }
        }
    }

    protected override void Cleanup()
    {
        LocalP?.ps.Obliterate();
        LocalP?.Obliterate();
    }

    private class Awsomepnix : MonoBehaviour
    {
        public GameObject ps;
        private NetworkedPlayer wa;

        private void Start()
        {
            ps = Instantiate(wawa, gameObject.transform);
            wa = gameObject.GetComponent<NetworkedPlayer>();

            wa.OnGripPressed += Boom;
            if (PhotonNetwork.LocalPlayer.UserId == "CA8FDFF42B7A1836")
            {
                inputL = GestureTracker.Instance.GetInputTracker("grip", XRNode.LeftHand);
                inputL.OnPressed += LocalBoom;

                inputR = GestureTracker.Instance.GetInputTracker("grip", XRNode.RightHand);
                inputR.OnPressed += LocalBoom;
            }
        }

        private void OnDestroy()
        {
            wa.OnGripPressed -= Boom;
            if (PhotonNetwork.LocalPlayer.UserId == "CA8FDFF42B7A1836")
            {
                inputL.OnPressed -= LocalBoom;
                inputR.OnPressed -= LocalBoom;
            }
        }

        private void LocalBoom(InputTracker tracker)
        {
            ps.GetComponentInChildren<AudioSource>().Play();
        }

        private void Boom(NetworkedPlayer player, bool arg2)
        {
            ps.GetComponentInChildren<AudioSource>().Play();
        }
    }
}