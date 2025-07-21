// TODO: Rewrite this cursed ass fucking stupid ass fuck ass bum ass poop ass fuckin ass fuckin module
// If either me or Graze touches this code AT ALL the networking just completely breaks and none of us know why
//////////// DON'T TOUCH IT UNLESS YOU'RE REWRITING THE WHOLE THING ////////////
// -- luna

using System.Collections.Generic;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using Grate.Patches;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;
using Random = System.Random;

namespace Grate.Modules.Misc;

internal class CatMeow : GrateModule
{
    private static readonly List<AudioClip> meowSounds = new();
    private static GameObject meowerPrefab;
    private static readonly Random rnd = new();
    private readonly InputTracker? inputL = GestureTracker.Instance.GetInputTracker("grip", XRNode.LeftHand);
    private readonly InputTracker? inputR = GestureTracker.Instance.GetInputTracker("grip", XRNode.RightHand);
    private AudioSource meowAudio;
    private GameObject meowbox;
    private ParticleSystem meowParticles;
    private VRRig rig;

    private void Awake()
    {
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
    }

    protected override void Start()
    {
        base.Start();
        try
        {
            rig = GorillaTagger.Instance.offlineVRRig;
            meowerPrefab = Plugin.assetBundle.LoadAsset<GameObject>("ParticleEmitter");
            meowbox = Instantiate(meowerPrefab, rig.gameObject.transform);
            meowbox.transform.localPosition = Vector3.zero;
            meowParticles = meowbox.GetComponent<ParticleSystem>();
            meowAudio = meowbox.GetComponent<AudioSource>();

            meowSounds.Add(Plugin.assetBundle.LoadAsset<AudioClip>("meow1"));
            meowSounds.Add(Plugin.assetBundle.LoadAsset<AudioClip>("meow2"));
            meowSounds.Add(Plugin.assetBundle.LoadAsset<AudioClip>("meow3"));
            meowSounds.Add(Plugin.assetBundle.LoadAsset<AudioClip>("meow4"));
        }
        catch
        {
        }
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built || PhotonNetwork.LocalPlayer.UserId != "FBE3EE50747CB892") return;
        base.OnEnable();
        GripOn();
    }

    protected override void OnDisable()
    {
        GripOff();
    }

    public static string DisplayName = "Meow";

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Mrrrrpp....";
    }

    protected override void Cleanup()
    {
        GripOff();
    }

    private void OnLocalGrip(InputTracker _)
    {
        DoMeow(meowParticles, meowAudio);
    }

    private void GripOn()
    {
        inputL.OnPressed += OnLocalGrip;
        inputR.OnPressed += OnLocalGrip;
    }

    private void GripOff()
    {
        inputL.OnPressed -= OnLocalGrip;
        inputR.OnPressed -= OnLocalGrip;
    }

    private void OnRigCached(NetPlayer player, VRRig rig)
    {
        rig?.gameObject?.GetComponent<TheMeower>()?.Obliterate();
    }

    private void OnPlayerModStatusChanged(NetPlayer player, string mod, bool enabled)
    {
        if (mod == GetDisplayName() && player.UserId == "FBE3EE50747CB892")
        {
            if (enabled)
                player.Rig().gameObject.GetOrAddComponent<TheMeower>();
            else
                Destroy(player.Rig().gameObject.GetComponent<TheMeower>());
        }
    }

    private static void DoMeow(ParticleSystem meowParticles, AudioSource meowAudioSource)
    {
        meowAudioSource.PlayOneShot(meowSounds[rnd.Next(meowSounds.Count)]);
        meowParticles.Play();
        meowParticles.Emit(1);
    }

    private class TheMeower : MonoBehaviour
    {
        private AudioSource meowAudioNet;
        private GameObject meowboxNet;
        private ParticleSystem meowParticlesNet;
        private NetworkedPlayer netPlayer;
        private VRRig rigNet;

        private void Start()
        {
            if (PhotonNetwork.LocalPlayer.UserId == "FBE3EE50747CB892")
            {
                rigNet = GetComponent<VRRig>();
                netPlayer = rigNet.GetComponent<NetworkedPlayer>();
                meowboxNet = Instantiate(meowerPrefab, rigNet.gameObject.transform);
                meowboxNet.transform.localPosition = Vector3.zero;
                meowParticlesNet = meowboxNet.GetComponent<ParticleSystem>();
                meowAudioNet = meowboxNet.GetComponent<AudioSource>();

                netPlayer.OnGripPressed += DoMeowNetworked;
            }
            else
            {
                Destroy(this);
            }
        }

        private void OnDestroy()
        {
            netPlayer.OnGripPressed -= DoMeowNetworked;
        }

        private void DoMeowNetworked(NetworkedPlayer player, bool isLeft)
        {
            DoMeow(meowParticlesNet, meowAudioNet);
        }
    }
}