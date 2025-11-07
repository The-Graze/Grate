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
    private GameObject Genesis;

    private void Start()
    {
        Genesis = Instantiate(NullGenesis.GenesisPrefab);
        Genesis.transform.SetParent(transform, false);
        Genesis.transform.localPosition = new Vector3(0, -.1f, 0);
    }

    private void OnDestroy()
    {
        Destroy(Genesis);
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
        if (enabled && player.IsDev())
            player.Rig().gameObject.GetOrAddComponent<GenesisMarker>();
        else
            Destroy(player.Rig().gameObject.GetComponent<GenesisMarker>());
    }

    protected override void Cleanup()
    {
        Genesis?.Obliterate();
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "The birth of nothing";
    }
}