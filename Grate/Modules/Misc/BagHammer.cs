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

public class BagHammer : GrateModule
{
    public static string DisplayName = "Bag Hammer";
    private static GameObject? Sword;

    protected override void Start()
    {
        base.Start();
        if (Sword == null)
        {
            Sword = Instantiate(Plugin.assetBundle.LoadAsset<GameObject>("bagHammer"));
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
        if (mod == DisplayName && player != NetworkSystem.Instance.LocalPlayer && player.UserId == "9ABD0C174289F58E")
        {
            if (enabled)
                player.Rig().gameObject.GetOrAddComponent<NetHammer>();
            else
                Destroy(player.Rig().gameObject.GetComponent<NetHammer>());
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
        rig?.gameObject?.GetComponent<NetHammer>()?.Obliterate();
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "baggZ";
    }

    private class NetHammer : MonoBehaviour
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