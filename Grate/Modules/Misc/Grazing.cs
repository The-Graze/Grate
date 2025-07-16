using Grate.Extensions;
using Grate.GUI;
using Grate.Networking;
using Grate.Patches;
using UnityEngine;
using UnityEngine.Video;

namespace Grate.Modules.Misc;

public class Grazing : GrateModule
{
    private GrazeHandler LocalGraze;
    public static GameObject? TV;
    private void Awake()
    {
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
        TV = Plugin.assetBundle?.LoadAsset<GameObject>("GrazeTV");
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        LocalGraze = GorillaTagger.Instance.offlineVRRig.AddComponent<GrazeHandler>();
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
        {
            rig?.gameObject?.GetComponent<GrazeHandler>()?.Obliterate();
        }
    }

    private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool modEnabled)
    {
        if (mod != GetDisplayName() || player.UserId != "42D7D32651E93866") return;
        
        if (modEnabled)
        {
            player.Rig()?.gameObject.GetOrAddComponent<GrazeHandler>();
        }
        else
        {
            player.Rig()?.gameObject.GetComponent<GrazeHandler>().Obliterate();
        }
    }

    protected override void Cleanup()
    {
        LocalGraze?.Obliterate();
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
        public VideoPlayer? vp;
        private NetworkedPlayer? np;

        private void Start()
        {
            np = GetComponent<NetworkedPlayer>();
            vp = Instantiate(TV, np.rig.head.headTransform.position, np.rig.head.headTransform.rotation)
                .GetComponentInChildren<VideoPlayer>();
            vp.loopPointReached += delegate { vp.Play(); };
        }

        private void Update()
        {
            if (!np) return;
            if (np?.owner?.UserId != "42D7D32651E93866")
            {
                this.Obliterate();
            }
        }
        private void OnDisable()
        {
            vp?.transform.parent.parent.gameObject.Obliterate();
        }
        private void OnDestroy()
        {
            vp?.transform.parent.parent.gameObject.Obliterate();
        }
    }
}