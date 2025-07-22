using System;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Interaction;
using Grate.Tools;
using UnityEngine;

namespace Grate.Modules.Movement;

public class GrapplingHooks : GrateModule
{
    public static readonly string DisplayName = "Grappling Hooks";

    public static ConfigEntry<int> Spring, Steering, MaxLength;
    public static ConfigEntry<string> RopeType;
    private readonly Vector3 holsterOffset = new(0.15f, -0.15f, 0.15f);
    private GameObject bananaGunPrefab, bananaGunL, bananaGunR;
    private Transform holsterL, holsterR;

    private void Awake()
    {
        try
        {
            bananaGunPrefab = Plugin.AssetBundle.LoadAsset<GameObject>("Banana Gun");
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
            if (!bananaGunPrefab)
                bananaGunPrefab = Plugin.AssetBundle.LoadAsset<GameObject>("Banana Gun");

            holsterL = new GameObject("Holster (Left)").transform;
            bananaGunL = Instantiate(bananaGunPrefab);
            SetupBananaGun(ref holsterL, ref bananaGunL, true);

            holsterR = new GameObject("Holster (Right)").transform;
            bananaGunR = Instantiate(bananaGunPrefab);
            SetupBananaGun(ref holsterR, ref bananaGunR, false);
            ReloadConfiguration();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void SetupBananaGun(ref Transform holster, ref GameObject bananaGun, bool isLeft)
    {
        try
        {
            holster.SetParent(GTPlayer.Instance.bodyCollider.transform, false);
            var offset = new Vector3(
                holsterOffset.x * (isLeft ? -1 : 1),
                holsterOffset.y,
                holsterOffset.z
            );
            holster.localPosition = offset;

            var gun = bananaGun.AddComponent<BananaGun>();
            gun.name = isLeft ? "Banana Grapple Left" : "Banana Grapple Right";
            gun.Holster(holster);
            gun.SetupInteraction();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    protected override void Cleanup()
    {
        try
        {
            holsterL?.gameObject?.Obliterate();
            holsterR?.gameObject?.Obliterate();
            bananaGunL?.gameObject?.Obliterate();
            bananaGunR?.gameObject?.Obliterate();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    protected override void ReloadConfiguration()
    {
        var guns = new[] { bananaGunL?.GetComponent<BananaGun>(), bananaGunR?.GetComponent<BananaGun>() };
        foreach (var gun in guns)
        {
            if (!gun) continue;
            gun.pullForce = Spring.Value * 2;
            gun.ropeType = RopeType.Value == "elastic" ? BananaGun.RopeType.ELASTIC : BananaGun.RopeType.STATIC;
            gun.steerForce = Steering.Value / 2f;
            gun.maxLength = MaxLength.Value * 5;
            Logging.Debug(
                "gun.pullForce:", gun.pullForce,
                "gun.ropeType:", gun.ropeType,
                "gun.steerForce:", gun.steerForce,
                "gun.maxLength:", gun.maxLength
            );
        }
    }

    public static void BindConfigEntries()
    {
        RopeType = Plugin.ConfigFile.Bind(
            DisplayName,
            "rope type",
            "elastic",
            new ConfigDescription(
                "Whether the rope should pull you to the anchor point or not",
                new AcceptableValueList<string>("elastic", "rope")
            )
        );

        Spring = Plugin.ConfigFile.Bind(
            DisplayName,
            "springiness",
            5,
            "If ropes are elastic, this is how springy the ropes are"
        );

        Steering = Plugin.ConfigFile.Bind(
            DisplayName,
            "steering",
            5,
            "How much influence you have over your velocity"
        );

        MaxLength = Plugin.ConfigFile.Bind(
            DisplayName,
            "max length",
            5,
            "The maximum distance that the grappling hook can reach"
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Grab the grappling hook off of your waist with [Grip]. " +
               "Then fire with [Trigger]. " +
               "You can steer in the air by pointing the guns where you want to go.";
    }
}

public class BananaGun : GrateGrabbable
{
    public enum RopeType
    {
        ELASTIC,
        STATIC
    }

    public Transform holster;
    public RopeType ropeType;

    public float
        pullForce = 10f,
        steerForce = 5f,
        maxLength = 30f;

    private float baseLaserWidth, baseRopeWidth;
    private Vector3 hitPosition;
    private bool isGrappling;

    private SpringJoint joint;
    private GameObject openModel, closedModel;
    private LineRenderer rope, laser;

    protected override void Awake()
    {
        base.Awake();
        LocalPosition = new Vector3(.55f, 0, .85f);
        openModel = transform.Find("Banana Gun Open").gameObject;
        closedModel = transform.Find("Banana Gun Closed").gameObject;
        //baseModelOffsetClosed = closedModel.transform.localPosition;
        //baseModelOffsetOpen = openModel.transform.localPosition;
        rope = openModel.GetComponentInChildren<LineRenderer>();
        rope.useWorldSpace = false;
        baseRopeWidth = rope.startWidth;
        laser = closedModel.GetComponentInChildren<LineRenderer>();
        laser.useWorldSpace = false;
        baseLaserWidth = laser.startWidth;
    }


    private void FixedUpdate()
    {
        if (Selected && !isGrappling && Activated)
        {
            StartSwing();
            return;
        }

        if (isGrappling)
        {
            var rigidBody = GTPlayer.Instance.bodyCollider.attachedRigidbody;
            rigidBody.velocity +=
                transform.forward *
                steerForce * Time.fixedDeltaTime * GTPlayer.Instance.scale;
        }
    }

    private void OnEnable()
    {
        Application.onBeforeRender += UpdateLineRenderer;
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= UpdateLineRenderer;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        joint?.Obliterate();
    }

    public void Holster(Transform holster)
    {
        Close();
        this.holster = holster;
        transform.SetParent(holster);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        if (laser)
            laser.enabled = false;
    }

    public override void OnActivate(GrateInteractor interactor)
    {
        base.OnActivate(interactor);
        Activated = true;
    }

    public override void OnDeactivate(GrateInteractor interactor)
    {
        base.OnDeactivate(interactor);
        Activated = false;
        Close();
    }

    private void StartSwing()
    {
        RaycastHit hit;
        var ray = new Ray(rope.transform.position, transform.forward);
        UnityEngine.Physics.SphereCast(ray, .5f * GTPlayer.Instance.scale, out hit, maxLength, Teleport.layerMask);
        if (!hit.transform) return;

        isGrappling = true;
        Open();
        rope.SetPosition(0, rope.transform.position);
        rope.SetPosition(1, hit.point);
        hitPosition = hit.point;

        joint = GTPlayer.Instance.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = hitPosition;

        var distanceFromPoint = Vector3.Distance(rope.transform.position, hitPosition);

        // the distance grapple will try to keep from grapple point. 
        switch (ropeType)
        {
            case RopeType.ELASTIC:
                joint.maxDistance = 0.8f;
                joint.minDistance = 0.25f;
                joint.spring = pullForce;
                joint.damper = 7f;
                joint.massScale = 4.5f;
                break;
            case RopeType.STATIC:
                joint.maxDistance = distanceFromPoint;
                joint.minDistance = distanceFromPoint;
                joint.spring = pullForce * 2;
                joint.damper = 100f;
                joint.massScale = 4.5f;
                break;
        }
    }

    private void UpdateLineRenderer()
    {
        if (!isGrappling && Selected)
        {
            RaycastHit hit;
            var ray = new Ray(rope.transform.position, transform.forward);
            UnityEngine.Physics.SphereCast(ray, .5f * GTPlayer.Instance.scale, out hit, maxLength, Teleport.layerMask);
            if (!hit.transform)
            {
                laser.enabled = false;
                return;
            }

            Vector3
                start = Vector3.zero,
                end = laser.transform.InverseTransformPoint(hit.point);

            laser.enabled = true;
            laser.SetPosition(0, start);
            laser.SetPosition(1, end);
            laser.startWidth = baseLaserWidth * GTPlayer.Instance.scale;
            laser.endWidth = baseLaserWidth * GTPlayer.Instance.scale;
        }
        else if (isGrappling)
        {
            Vector3
                start = Vector3.zero,
                end = rope.transform.InverseTransformPoint(hitPosition);
            rope.SetPosition(0, start);
            rope.SetPosition(1, end);
            rope.startWidth = baseRopeWidth * GTPlayer.Instance.scale;
            rope.endWidth = baseRopeWidth * GTPlayer.Instance.scale;
        }
    }

    public override void OnDeselect(GrateInteractor interactor)
    {
        base.OnDeselect(interactor);
        laser.enabled = false;
        Holster(holster);
    }

    public void SetupInteraction()
    {
        throwOnDetach = false;
        gameObject.layer = GrateInteractor.InteractionLayer;
        if (openModel)
            openModel.layer = GrateInteractor.InteractionLayer;
        if (closedModel)
            closedModel.layer = GrateInteractor.InteractionLayer;
    }

    private void Open()
    {
        openModel?.SetActive(true);
        closedModel?.SetActive(false);
        GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(96, false, 0.05f);
    }

    private void Close()
    {
        openModel?.SetActive(false);
        closedModel?.SetActive(true);
        Activated = false;
        isGrappling = false;
        joint?.Obliterate();
    }
}