using Grate.Extensions;
using UnityEngine;
using UnityEngine;
﻿using Grate.GUI;
using Grate.Networking;
using Grate.Patches;
using UnityEngine.Video;

namespace Grate.Modules.Misc;

public class Grazing : GrateModule
{
    private static GameObject? _tv;
    private GrazeHandler? localGraze;

    protected override void Start()
    {
        base.Start();
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
        _tv = Plugin.AssetBundle?.LoadAsset<GameObject>("GrazeTV");
        _tv?.transform.GetChild(1).AddComponent<MuteButton>();
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        localGraze = GorillaTagger.Instance.offlineVRRig.AddComponent<GrazeHandler>();
    }

    public override string GetDisplayName()
    {
        return "Gwazywazy";
    }

    public override string Tutorial()
    {
        return "I am me maker of this yes";
    }

    private void OnRigCached(NetPlayer player, VRRig rig)
    {
        if (rig?.gameObject?.GetComponent<GrazeHandler>() != null)
            rig?.gameObject?.GetComponent<GrazeHandler>()?.Obliterate();
    }

    private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool modEnabled)
    {
        if (mod != GetDisplayName() || player.UserId != "42D7D32651E93866") return;

        if (modEnabled)
            player.Rig()?.gameObject.GetOrAddComponent<GrazeHandler>();
        else
            player.Rig()?.gameObject.GetComponent<GrazeHandler>().Obliterate();
    }

    protected override void Cleanup()
    {
        localGraze?.Obliterate();
    }

    public class MuteButton : GorillaPressableButton
    {
        private bool muted;

        public override void ButtonActivation()
        {
            base.ButtonActivation();
            muted = !muted;
            transform.parent.GetComponentInChildren<AudioSource>().mute = muted;
        }
    }

    public class GrazeHandler : MonoBehaviour
    {
        private NetworkedPlayer? np;
        private GameObject? tv;
        public VideoPlayer? vp;

        private void Start()
        {
            np = GetComponent<NetworkedPlayer>();
            tv = Instantiate(_tv);
            tv.transform.position = np.rig.headConstraint.position + Vector3.up * .25f + Vector3.forward * .25f;
            tv.transform.rotation = np.rig.syncRotation;
            vp = tv?.GetComponentInChildren<VideoPlayer>()
                .GetComponentInChildren<VideoPlayer>();
            vp.loopPointReached += delegate { vp.Play(); };
        }

        private void Update()
        {
            if (!np) return;
            if (np?.owner?.UserId != "42D7D32651E93866") this.Obliterate();
        }

        private void OnDisable()
        {
            OnDestroy();
        }

        private void OnDestroy()
        {
            tv?.Obliterate();
        }
    }
}