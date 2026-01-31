using UnityEngine;
using UnityEngine;
﻿using System;
using System.Collections.Generic;
using Grate.Extensions;
using Grate.GUI;
using Grate.Networking;
using Grate.Patches;
using Grate.Tools;

namespace Grate.Modules.Misc;

internal class MusicVis : GrateModule
{
    private VisMarker Marker;

    private void Awake()
    {
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            Marker = GorillaTagger.Instance.offlineVRRig.gameObject.AddComponent<VisMarker>();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    protected override void OnDisable()
    {
        Destroy(Marker);
    }

    public override string GetDisplayName()
    {
        return "Music Vis";
    }

    public override string Tutorial()
    {
        return "Graze Proof, I love music visualising";
    }

    protected override void Cleanup()
    {
        Marker.Obliterate();
    }

    private void OnRigCached(NetPlayer player, VRRig rig)
    {
        rig?.gameObject?.GetComponent<VisMarker>()?.Obliterate();
    }

    private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool enabled)
    {
        if (mod == GetDisplayName() && player.UserId == "E5F14084F14ED3CE")
        {
            if (enabled)
                player.Rig().gameObject.GetOrAddComponent<VisMarker>();
            else
                Destroy(player.Rig().gameObject.GetComponent<VisMarker>());
        }
    }
}

internal class VisMarker : MonoBehaviour
{
    private Transform anc;
    private VRRig rig;
    private GorillaSpeakerLoudness Speakerloudness;
    private List<Transform> VisParts;

    private void Start()
    {
        rig = GetComponent<VRRig>();
        anc = new GameObject("Vis").transform;
        VisParts = new List<Transform>();
        for (var i = 0; i < 50; i++)
        {
            var wawa = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            wawa.GetComponent<Collider>().Obliterate();
            wawa.GetComponent<Renderer>().material = MenuController.Instance.grate[1];
            wawa.transform.SetParent(anc, false);
            wawa.transform.localScale = new Vector3(0.11612f, 0.11612f, 0.11612f);
            VisParts.Add(wawa.transform);
            Debug.Log($"{i} shperes made");
        }
    }

    private void FixedUpdate()
    {
        if (Speakerloudness == null) Speakerloudness = rig.GetComponent<GorillaSpeakerLoudness>();
        if (anc.parent == null)
        {
            anc.SetParent(rig.transform, false);
        }
        else if (VisParts.Count == 50)
        {
            var count = VisParts.Count;
            var num = 360f / count;
            var currentLoudness = Speakerloudness.SmoothedLoudness;
            var position = anc.transform.position;
            for (var i = 0; i < count; i++)
            {
                var num2 = i * num;
                var x = currentLoudness * Mathf.Cos(num2 * 0.017453292f);
                var z = currentLoudness * Mathf.Sin(num2 * 0.017453292f);
                var vector = position + new Vector3(x, 0.2f, z);
                var y = vector.y + currentLoudness;
                var position2 = new Vector3(vector.x, y, vector.z);
                VisParts[i].transform.position = position2;
                VisParts[i].transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }
    }

    private void OnDisable()
    {
        anc.Obliterate();
    }

    private void OnDestory()
    {
        anc.Obliterate();
    }
}