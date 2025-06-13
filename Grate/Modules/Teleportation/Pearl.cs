using System;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Interaction;
using Grate.Patches;
using Grate.Tools;
using UnityEngine;

namespace Grate.Modules.Teleportation;

public class Pearl : GrateModule
{
    public static readonly string DisplayName = "Pearl";
    public static Pearl Instance;

    public static ConfigEntry<int> ThrowForce;
    private ThrowablePearl pearl;
    private GameObject pearlPrefab;

    private void Awake()
    {
        try
        {
            Instance = this;
            pearlPrefab = Plugin.assetBundle.LoadAsset<GameObject>("Pearl");
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        Setup();
    }

    private void Setup()
    {
        try
        {
            pearl = SetupPearl(Instantiate(pearlPrefab), false);
            ReloadConfiguration();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private ThrowablePearl SetupPearl(GameObject pearlObj, bool isLeft)
    {
        try
        {
            pearlObj.name = "Grate Pearl";
            var pearl = pearlObj.AddComponent<ThrowablePearl>();
            return pearl;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }

        return null;
    }

    protected override void Cleanup()
    {
        try
        {
            pearl?.gameObject?.Obliterate();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    protected override void ReloadConfiguration()
    {
        pearl.throwForceMultiplier = ThrowForce.Value;
    }

    public static void BindConfigEntries()
    {
        ThrowForce = Plugin.configFile.Bind(
            DisplayName,
            "throw force",
            5,
            "How much to multiply the throw speed by on release"
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Hold either [Grip] to summon a pearl. Throw it and you will to teleport where it lands.";
    }
}

public class ThrowablePearl : GrateGrabbable
{
    private AudioSource audioSource;
    private GestureTracker gt;
    private LayerMask mask;
    private Material monkeMat, trailMat;
    private VRRig playerRig;

    private Ray ray;
    private Rigidbody rigidbody;
    private bool thrown, landed = true;
    private ParticleSystem trail;

    protected override void Awake()
    {
        try
        {
            base.Awake();
            throwOnDetach = true;
            throwForceMultiplier = 5;
            LocalRotation = new Vector3(0, -90f, 0);
            LocalPosition = Vector3.right * .8f;
            monkeMat = GetComponentInChildren<SkinnedMeshRenderer>().material;
            trailMat = GetComponentInChildren<ParticleSystemRenderer>().material;
            trail = GetComponentInChildren<ParticleSystem>();
            gameObject.layer = GrateInteractor.InteractionLayer;
            rigidbody = gameObject.GetOrAddComponent<Rigidbody>();
            rigidbody.useGravity = true;
            gt = GestureTracker.Instance;
            gt.rightGrip.OnPressed += Attach;
            gt.leftGrip.OnPressed += Attach;
            mask = GTPlayer.Instance.locomotionEnabledLayers;
            audioSource = gameObject.GetComponent<AudioSource>();
            playerRig = GorillaTagger.Instance.offlineVRRig;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void FixedUpdate()
    {
        if (!thrown) return;
        ray.origin = transform.position;
        ray.direction = rigidbody.velocity;
        RaycastHit hit;
        UnityEngine.Physics.Raycast(ray, out hit, ray.direction.magnitude, mask);

        if (hit.collider != null)
        {
            var position = transform.position;
            var vector = Camera.main.transform.position - position;
            var wawa = hit.point + hit.normal * GTPlayer.Instance.scale / 2f;
            var position2 = wawa - vector;
            TeleportPatch.TeleportPlayer(wawa, 0);
            audioSource.Play();
            thrown = false;
            landed = true;
            trail.Stop();
            transform.position = Vector3.down * 1000;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (!gt) return;
        gt.leftGrip.OnPressed -= Attach;
        gt.rightGrip.OnPressed -= Attach;
    }

    private void Attach(InputTracker tracker)
    {
        try
        {
            var isLeft = tracker == gt.leftGrip;
            var parent = isLeft ? gt.leftPalmInteractor : gt.rightPalmInteractor;
            if (!CanBeSelected(parent)) return;
            float dir = isLeft ? 1 : -1;
            transform.parent = null;
            transform.localScale = Vector3.one * GTPlayer.Instance.scale * .1f;
            LocalRotation = new Vector3(0, 90f * dir, 0);
            parent.Select(this);

            monkeMat.color = playerRig.playerColor;
            trailMat.color = playerRig.playerColor;
            trail.Stop();
            Sounds.Play(Sounds.Sound.crystalhandtap, .05f);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    public override void OnDeselect(GrateInteractor interactor)
    {
        base.OnDeselect(interactor);
        thrown = true;
        trail.Play();
    }

    public void SetupInteraction()
    {
    }
}