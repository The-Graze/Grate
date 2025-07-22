using System;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Interaction;
using Grate.Tools;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Grate.Modules.Movement;

public class Rockets : GrateModule
{
    public static readonly string DisplayName = "Rockets";
    public static Rockets Instance;

    public static ConfigEntry<int> Power;
    public static ConfigEntry<int> Volume;
    private Rocket rocketL, rocketR;
    private GameObject rocketPrefab;

    private void Awake()
    {
        try
        {
            Instance = this;
            rocketPrefab = Plugin.AssetBundle.LoadAsset<GameObject>("Rocket");
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
            if (!rocketPrefab)
                rocketPrefab = Plugin.AssetBundle.LoadAsset<GameObject>("Rocket");

            rocketL = SetupRocket(Instantiate(rocketPrefab), true);
            rocketR = SetupRocket(Instantiate(rocketPrefab), false);
            ReloadConfiguration();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private Rocket SetupRocket(GameObject rocketObj, bool isLeft)
    {
        try
        {
            rocketObj.name = isLeft ? "Grate Rocket Left" : "Grate Rocket Right";
            var rocket = rocketObj.AddComponent<Rocket>().Init(isLeft);
            rocket.LocalPosition = new Vector3(0.51f, -3, 0f);
            rocket.LocalRotation = new Vector3(0, 0, -90);
            return rocket;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }

        return null;
    }

    public Vector3 AddedVelocity()
    {
        return rocketL.force + rocketR.force;
    }

    protected override void Cleanup()
    {
        try
        {
            rocketL?.gameObject?.Obliterate();
            rocketR?.gameObject?.Obliterate();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    protected override void ReloadConfiguration()
    {
        var rockets = new[] { rocketL?.GetComponent<Rocket>(), rocketR?.GetComponent<Rocket>() };
        foreach (var rocket in rockets)
        {
            if (!rocket) continue;
            rocket.power = Power.Value * 2f;
            rocket.volume = MathExtensions.Map(Volume.Value, 0, 10, 0, 1);
        }
    }

    public static void BindConfigEntries()
    {
        Power = Plugin.ConfigFile.Bind(
            DisplayName,
            "power",
            5,
            "The power of each rocket"
        );

        Volume = Plugin.ConfigFile.Bind(
            DisplayName,
            "thruster volume",
            10,
            "How loud the thrusters sound"
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Hold either [Grip] to summon a rocket.";
    }
}

public class Rocket : GrateGrabbable
{
    public float power = 5f, volume = .2f;
    public AudioSource exhaustSound;
    private GestureTracker gt;
    private bool isLeft;
    private Rigidbody rb;
    public Vector3 force { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        exhaustSound = GetComponent<AudioSource>();
        exhaustSound.Stop();
    }

    private void FixedUpdate()
    {
        var player = GTPlayer.Instance;
        force = transform.forward * power * Time.fixedDeltaTime * GTPlayer.Instance.scale;
        if (Selected)
        {
            player.AddForce(force);
        }
        else
        {
            rb.velocity += force * 10;
            force = Vector3.zero;
            transform.Rotate(Random.insideUnitSphere);
        }

        exhaustSound.volume = Mathf.Lerp(.5f, 0, Vector3.Distance(
            player.headCollider.transform.position,
            transform.position
        ) / 20f) * volume;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (!gt) return;
        gt.leftGrip.OnPressed -= Attach;
        gt.rightGrip.OnPressed -= Attach;
    }

    public Rocket Init(bool isLeft)
    {
        this.isLeft = isLeft;
        gt = GestureTracker.Instance;

        if (isLeft)
            gt.leftGrip.OnPressed += Attach;
        else
            gt.rightGrip.OnPressed += Attach;
        return this;
    }

    private void Attach(InputTracker _)
    {
        var parent = isLeft ? gt.leftPalmInteractor : gt.rightPalmInteractor;
        if (!CanBeSelected(parent)) return;
        transform.parent = null;
        transform.localScale = Vector3.one * GTPlayer.Instance.scale;
        parent.Select(this);
        exhaustSound.Stop();
        exhaustSound.time = Random.Range(0, exhaustSound.clip.length);
        exhaustSound.Play();
    }

    public override void OnDeselect(GrateInteractor interactor)
    {
        base.OnDeselect(interactor);
        rb.velocity = GTPlayer.Instance.GetComponent<Rigidbody>().velocity;
    }

    public void SetupInteraction()
    {
        throwOnDetach = true;
        gameObject.layer = GrateInteractor.InteractionLayer;
    }
}