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

public class RatSword : GrateModule
{
    public static readonly string DisplayName = "Rat Sword";
    private static GameObject? Sword;

    protected override void Start()
    {
        base.Start();
        if (Sword == null)
        {
            Sword = Instantiate(Plugin.assetBundle.LoadAsset<GameObject>("Rat Sword"));
            Sword.transform.SetParent(GestureTracker.Instance.rightHand.transform, true);
            Sword.transform.localPosition = new Vector3(-0.4782f, 0.1f, 0.4f);
            Sword.transform.localRotation = Quaternion.Euler(9, 0, 0);
            Sword.transform.localScale /= 2;
            Sword.SetActive(false);
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
            GestureTracker.Instance.rightGrip.OnPressed += ToggleRatSwordOn;
            GestureTracker.Instance.rightGrip.OnReleased += ToggleRatSwordOff;
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
                player.Rig().gameObject.GetOrAddComponent<NetSword>();
            else
                Destroy(player.Rig().gameObject.GetComponent<NetSword>());
        }
    }


    private void ToggleRatSwordOn(InputTracker tracker)
    {
        Sword?.SetActive(true);
    }

    private void ToggleRatSwordOff(InputTracker tracker)
    {
        Sword?.SetActive(false);
    }

    protected override void Cleanup()
    {
        Sword?.SetActive(false);
        if (GestureTracker.Instance != null)
        {
            GestureTracker.Instance.rightGrip.OnPressed -= ToggleRatSwordOn;
            GestureTracker.Instance.rightGrip.OnReleased -= ToggleRatSwordOff;
        }
    }

    private void OnRigCached(NetPlayer player, VRRig rig)
    {
        rig?.gameObject?.GetComponent<NetSword>()?.Obliterate();
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "I met a lil' kid in canyons who wanted kyle to make him a sword.\n" +
               "[Grip] to wield your weapon, rat kid.";
    }

    private class NetSword : MonoBehaviour
    {
        private NetworkedPlayer networkedPlayer;
        private GameObject sword;

        private void OnEnable()
        {
            networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
            var rightHand = networkedPlayer.rig.rightHandTransform;

            sword = Instantiate(Sword);

            sword.transform.SetParent(rightHand);
            sword.transform.localPosition = new Vector3(0.04f, 0.05f, -0.02f);
            sword.transform.localRotation = Quaternion.Euler(78.4409f, 0, 0);
            sword.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            networkedPlayer.OnGripPressed += OnGripPressed;
            networkedPlayer.OnGripReleased += OnGripReleased;
        }

        private void OnDisable()
        {
            sword.Obliterate();
            networkedPlayer.OnGripPressed -= OnGripPressed;
            networkedPlayer.OnGripReleased -= OnGripReleased;
        }

        private void OnDestroy()
        {
            sword.Obliterate();
            networkedPlayer.OnGripPressed -= OnGripPressed;
            networkedPlayer.OnGripReleased -= OnGripReleased;
        }

        private void OnGripPressed(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft) sword.SetActive(true);
        }

        private void OnGripReleased(NetworkedPlayer player, bool isLeft)
        {
            if (!isLeft) sword.SetActive(false);
        }
    }
}