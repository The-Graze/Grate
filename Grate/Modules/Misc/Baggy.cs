using UnityEngine;
using UnityEngine;
﻿using System;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using Grate.Patches;
using Grate.Tools;
using NetworkPlayer = NetPlayer;

namespace Grate.Modules.Misc;

public class Baggy : GrateModule
{
    public static string DisplayName = "Bag";
    private static GameObject? Bag;

    protected override void Start()
    {
        base.Start();
        if (Bag == null)
        {
            Bag = Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>("Bag"));
            Bag.transform.SetParent(GestureTracker.Instance.rightHand.transform, true);
            Bag.transform.localRotation = Quaternion.Euler(9, 0, 0);
            Bag.transform.localScale /= 4;
            Bag.SetActive(false);
        }

        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            GestureTracker.Instance.rightGrip.OnPressed += ToggleBagOn;
            GestureTracker.Instance.rightGrip.OnReleased += ToggleBagOff;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool enabled)
    {
        if (mod == DisplayName && player != NetworkSystem.Instance.LocalPlayer && player.UserId == "9ABD0C174289F58E")
        {
            if (enabled)
                player.Rig().gameObject.GetOrAddComponent<NetBag>();
            else
                Destroy(player.Rig().gameObject.GetComponent<NetBag>());
        }
    }


    private void ToggleBagOn(InputTracker tracker)
    {
        Bag?.SetActive(true);
    }

    private void ToggleBagOff(InputTracker tracker)
    {
        Bag?.SetActive(false);
    }

    protected override void Cleanup()
    {
        Bag?.SetActive(false);
        if (GestureTracker.Instance != null)
        {
            GestureTracker.Instance.rightGrip.OnPressed -= ToggleBagOn;
            GestureTracker.Instance.rightGrip.OnReleased -= ToggleBagOff;
        }
    }

    private void OnRigCached(NetPlayer player, VRRig rig)
    {
        rig?.gameObject?.GetComponent<NetBag>()?.Obliterate();
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "baggZ";
    }

    private class NetBag : MonoBehaviour
    {
        private GameObject Bag;
        private NetworkedPlayer networkedPlayer;

        private void OnEnable()
        {
            networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
            var rightHand = networkedPlayer.rig.rightHandTransform;

            Bag = Instantiate<GameObject>(Baggy.Bag);

            Bag.transform.SetParent(rightHand);
            Bag.transform.localPosition = new Vector3(0.04f, 0.05f, -0.02f);
            Bag.transform.localRotation = Quaternion.Euler(270, 163.12f, 0);
            Bag.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            networkedPlayer.OnGripPressed += OnGripPressed;
            networkedPlayer.OnGripReleased += OnGripReleased;
        }

        private void OnDisable()
        {
            Bag.Obliterate();
            networkedPlayer.OnGripPressed -= OnGripPressed;
            networkedPlayer.OnGripReleased -= OnGripReleased;
        }

        private void OnDestroy()
        {
            Bag.Obliterate();
            networkedPlayer.OnGripPressed -= OnGripPressed;
            networkedPlayer.OnGripReleased -= OnGripReleased;
        }

        private void OnGripPressed(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft) Bag.SetActive(true);
        }

        private void OnGripReleased(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft) Bag.SetActive(false);
        }
    }
}