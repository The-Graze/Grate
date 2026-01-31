using UnityEngine;
using UnityEngine;
﻿using System;
using Grate.Extensions;
using Grate.GUI;
using Grate.Tools;
using NetworkPlayer = NetPlayer;

namespace Grate.Modules.Movement;

public class GenesisMarker : MonoBehaviour
{
    private GameObject Genesis;

    private void Start()
    {
        Genesis = Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>("NULLG Prefab"));
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
    public Vector3 GenesisOffset;
    public Vector3 targetPosition;

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