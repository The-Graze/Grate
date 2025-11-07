using System;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using Grate.Patches;
using Grate.Tools;
using UnityEngine;
using NetworkPlayer = NetPlayer;

namespace Grate.Modules.Movement;

public class GenesisMarker : MonoBehaviour
{
    private GameObject bubble;

    private void Start()
    {
        bubble = Instantiate(Bubble.bubblePrefab);
        bubble.transform.SetParent(transform, false);
        bubble.transform.localPosition = new Vector3(0, -.1f, 0);
        bubble.gameObject.layer = GrateInteractor.InteractionLayer;
    }

    private void OnDestroy()
    {
        Destroy(bubble);
    }
}

public class NullGenesis : GrateModule
{
    public static readonly string DisplayName = "Null Genesis";
    public static GameObject GenesisPrefab;
    public GameObject Genesis;
    public Vector3 targetPosition;
    public Vector3 GenesisOffset;

    private void LateUpdate()
    {
        if (Genesis != null)
        {
            Genesis.transform.position = GTPlayer.Instance.headCollider.transform.position;
            Genesis.transform.position -= GenesisOffset * GTPlayer.Instance.scale;

            Vector3 leftPos = GestureTracker.Instance.leftHand.transform.position,
                rightPos = GestureTracker.Instance.rightHand.transform.position;
        }
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            ReloadConfiguration();

        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void OnRigCached(NetPlayer player, VRRig rig)
    {
        rig?.gameObject?.GetComponent<GenesisMarker>()?.Obliterate();
    }

    private void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool enabled)
    {
        if (mod != DisplayName) return;
        if (enabled)
            player.Rig().gameObject.GetOrAddComponent<GenesisMarker>();
        else
            Destroy(player.Rig().gameObject.GetComponent<GenesisMarker>());
    }

    public static void BindConfigEntries()
    {
        BubbleSize = Plugin.ConfigFile.Bind(
            DisplayName,
            "Genesis size",
            5,
            "How far you have to reach to hit the Genesis"
        );
        BubbleSpeed = Plugin.ConfigFile.Bind(
            DisplayName,
            "Genesis speed",
            5,
            "How fast the Genesis moves when you push it"
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Creates a Genesis around you so you can float. " +
               "Tap the side that you want to move towards to move.";
    }
}