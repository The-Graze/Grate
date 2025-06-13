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

    private void Awake()
    {
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
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
            rig?.gameObject?.GetComponent<GrazeHandler>()?.vp.Obliterate();
            rig?.gameObject?.GetComponent<GrazeHandler>()?.Obliterate();
        }
    }

    private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool enabled)
    {
        if (mod == GetDisplayName() && player.UserId == "42D7D32651E93866")
        {
            if (enabled)
            {
                player.Rig().gameObject.GetOrAddComponent<GrazeHandler>();
            }
            else
            {
                player.Rig().gameObject.GetComponent<GrazeHandler>().vp.gameObject.Obliterate();
                player.Rig().gameObject.GetComponent<GrazeHandler>().Obliterate();
            }
        }
    }

    protected override void Cleanup()
    {
        LocalGraze?.vp.Obliterate();
        LocalGraze?.Obliterate();
    }

    public class GrazeHandler : MonoBehaviour
    {
        public VideoPlayer vp;
        private NetworkedPlayer np;

        private void Start()
        {
            vp = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<VideoPlayer>();
            vp.transform.localScale = new Vector3(2, 1, 0.01f);
            vp.GetComponent<Collider>().Obliterate();
            vp.GetComponent<Renderer>().material.shader = Shader.Find("Universal Render Pipeline/Lit");
            vp.source = VideoSource.Url;
            vp.targetMaterialRenderer = vp.GetComponent<Renderer>();
            vp.url = "https://graze.gay/vid.mp4";
            vp.loopPointReached += delegate { vp.Play(); };
            vp.Play();
            vp.transform.SetParent(transform);
            vp.transform.localPosition = new Vector3(0, 1, 0);
            vp.transform.localRotation = Quaternion.Euler(Vector3.zero);
        }

        private void Update()
        {
            if (np == null)
            {
                np = gameObject.GetComponent<NetworkedPlayer>();
            }
            else
            {
                if (np.owner.UserId != "42D7D32651E93866")
                {
                    vp.gameObject.Obliterate();
                    this.Obliterate();
                }
            }
        }

        private void OnDisable()
        {
            vp.gameObject.Obliterate();
        }

        private void OnDestroy()
        {
            vp.gameObject.Obliterate();
        }
    }
}