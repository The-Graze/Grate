using System;
using BepInEx.Configuration;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Modules.Physics;
using Grate.Networking;
using Grate.Tools;
using UnityEngine;
using UnityEngine.XR;

namespace Grate.Modules.Movement;

public class Platform : MonoBehaviour
{
    public bool isSticky, isActive, isLeft;
    public GorillaClimbable Climbable;
    private Material cloudMaterial;
    private Collider collider;
    private Transform hand;
    private GameObject model;
    private string modelName;
    private ParticleSystem rain;
    private Vector3 scale;
    private float spawnTime;
    private Transform wings;


    public bool Sticky
    {
        set => isSticky = value;
    }

    public float Scale
    {
        set => scale = new Vector3(isLeft ? -1 : 1, 1, 1) * value;
    }


    public string Model
    {
        get => modelName;
        set
        {
            modelName = value;
            var path = modelName;
            if (modelName.Contains("cloud")) path = "cloud";
            model = transform.Find(path).gameObject;
            transform.Find("cloud").gameObject.SetActive(path == "cloud");
            transform.Find("doug").gameObject.SetActive(path == "doug");
            transform.Find("invisible").gameObject.SetActive(path == "invisible");
            transform.Find("ice").gameObject.SetActive(path == "ice");
            collider = model.GetComponent<BoxCollider>();
        }
    }

    private void FixedUpdate()
    {
        if (isActive)
        {
            spawnTime = Time.time;

            var transparency = Mathf.Clamp((Time.time - spawnTime) / 1f, 0.2f, 1);
            var c = modelName == "storm cloud" ? .2f : 1;
            cloudMaterial.color = new Color(c, c, c, Mathf.Lerp(1, 0, transparency));
            if (model.name == "doug")
                wings.transform.localRotation = Quaternion.Euler(Time.frameCount % 2 == 0 ? -30 : 0, 0, 0);
        }
    }

    public void Initialize(bool isLeft)
    {
        try
        {
            this.isLeft = isLeft;
            name = "Grate Platform " + (isLeft ? "Left" : "Right");
            Scale = 1;
            foreach (Transform child in transform)
            {
                child.gameObject.AddComponent<GorillaSurfaceOverride>().overrideIndex = 110;
                var cloud = transform.Find("cloud");
                cloudMaterial = cloud.GetComponent<Renderer>().material;
                cloudMaterial.color = new Color(1, 1, 1, 0);
                rain = cloud.GetComponent<ParticleSystem>();
                wings = transform.Find("doug/wings");
            }

            var handObj = isLeft
                ? GTPlayer.Instance.leftControllerTransform
                : GTPlayer.Instance.rightControllerTransform;
            hand = handObj.transform;
            Climbable = CreateClimbable();
            Climbable.transform.SetParent(transform);
            Climbable.transform.localPosition = Vector3.zero;
            rain.loop = true;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    public void Activate()
    {
        isActive = true;
        spawnTime = Time.time;
        transform.position = hand.transform.position;
        transform.rotation = hand.transform.rotation;
        transform.localScale = scale * GTPlayer.Instance.scale;
        collider.gameObject.layer = NoClip.active ? NoClip.layer : 0;
        collider.gameObject.layer = NoClip.active ? NoClip.layer : 0;
        collider.enabled = !isSticky;
        Climbable.gameObject.SetActive(isSticky);
        model.SetActive(true);
        if (modelName == "storm cloud") rain.Play();
    }

    public GorillaClimbable CreateClimbable()
    {
        var climbable = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        climbable.name = "Grate Climb Obj";
        climbable.AddComponent<GorillaClimbable>();
        climbable.layer = LayerMask.NameToLayer("GorillaInteractable");
        climbable.GetComponent<Renderer>().enabled = false;
        climbable.transform.localScale = Vector3.one * .15f;
        return climbable.GetComponent<GorillaClimbable>();
    }

    public void Deactivate()
    {
        isActive = false;
        collider.enabled = false;
        Climbable.gameObject.SetActive(false);
        model.SetActive(false);
    }
}

public class Platforms : GrateModule
{
    public static readonly string DisplayName = "Platforms";
    public static GameObject platformPrefab;
    public static GorillaHandClimber LeftC, RightC;
    private static Vector3 lastPositionL = Vector3.zero;
    private static Vector3 lastPositionR = Vector3.zero;
    private static Vector3 lastPositionHead = Vector3.zero;
    private static bool lHappen;
    private static bool rHappen;
    private static bool isVelocity;

    public static ConfigEntry<bool> Sticky;
    public static ConfigEntry<int> Scale;
    public static ConfigEntry<string> Input;
    public static ConfigEntry<string> Model;
    public Platform left, right, main;
    private InputTracker? inputL;
    private InputTracker? inputR;

    private void Awake()
    {
        if (!platformPrefab) platformPrefab = Plugin.assetBundle.LoadAsset<GameObject>("Bark Platform");
        foreach (var ghc in Resources.FindObjectsOfTypeAll<GorillaHandClimber>())
        {
            if (ghc.xrNode == XRNode.LeftHand) LeftC = ghc;
            if (ghc.xrNode == XRNode.RightHand) RightC = ghc;
        }
    }

    private void FixedUpdate()
    {
        if (isVelocity)
        {
            var threshold = 0.05f;

            var headMovementDelta = GorillaTagger.Instance.headCollider.transform.position - lastPositionHead;
            var leftHandMovementDelta = GorillaTagger.Instance.leftHandTransform.position - lastPositionL;
            var rightHandMovementDelta = GorillaTagger.Instance.rightHandTransform.position - lastPositionR;

            var leftHandMovingWithHead =
                Vector3.Dot(headMovementDelta.normalized, leftHandMovementDelta.normalized) > 0.4f;
            var rightHandMovingWithHead =
                Vector3.Dot(headMovementDelta.normalized, rightHandMovementDelta.normalized) > 0.4f;

            if (!leftHandMovingWithHead)
            {
                if (GorillaTagger.Instance.leftHandTransform.position.y + threshold <= lastPositionL.y)
                {
                    if (!lHappen)
                    {
                        lHappen = true;

                        OnActivate(inputL);
                    }
                }
                else
                {
                    if (lHappen)
                    {
                        lHappen = false;
                        OnDeactivate(inputL);
                    }
                }
            }

            if (!rightHandMovingWithHead)
            {
                if (GorillaTagger.Instance.rightHandTransform.position.y + threshold <= lastPositionR.y)
                {
                    if (!rHappen)
                    {
                        rHappen = true;

                        OnActivate(inputR);
                    }
                }
                else
                {
                    if (rHappen)
                    {
                        rHappen = false;
                        OnDeactivate(inputR);
                    }
                }
            }

            lastPositionL = GorillaTagger.Instance.leftHandTransform.position;
            lastPositionR = GorillaTagger.Instance.rightHandTransform.position;
            lastPositionHead = GorillaTagger.Instance.headCollider.transform.position;
        }
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            left = CreatePlatform(true);
            right = CreatePlatform(false);
            ReloadConfiguration();
            Plugin.menuController.GetComponent<Frozone>().button.AddBlocker(ButtonController.Blocker.MOD_INCOMPAT);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    public Platform CreatePlatform(bool isLeft)
    {
        var platformObj = Instantiate(platformPrefab);
        var platform = platformObj.AddComponent<Platform>();
        platform.Initialize(isLeft);
        return platform;
    }

    public void OnActivate(InputTracker? tracker)
    {
        if (enabled)
        {
            var isLeft = tracker.node == XRNode.LeftHand;

            main = isLeft ? left : right;

            var other = !isLeft ? left : right;

            main.Activate();

            if (Sticky.Value)
            {
                GTPlayer.Instance.bodyCollider.attachedRigidbody.velocity = Vector3.zero;
                other.Deactivate();
            }
        }
    }

    public void OnDeactivate(InputTracker? tracker)
    {
        var isLeft = tracker.node == XRNode.LeftHand;

        var platform = isLeft ? left : right;

        platform.Deactivate();
    }

    protected override void Cleanup()
    {
        if (left != null)
        {
            GTPlayer.Instance.EndClimbing(LeftC, false);
            left.gameObject?.Obliterate();
        }

        if (right != null)
        {
            GTPlayer.Instance.EndClimbing(RightC, false);
            right.gameObject?.Obliterate();
        }

        Unsub();
        Plugin.menuController.GetComponent<Frozone>().button.RemoveBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }

    protected override void ReloadConfiguration()
    {
        left.Model = Model.Value;
        right.Model = Model.Value;
        left.Sticky = Sticky.Value;
        right.Sticky = Sticky.Value;

        var scale = MathExtensions.Map(Scale.Value, 0, 10, .5f, 1.5f);
        left.Scale = scale;
        right.Scale = scale;

        Unsub();
        if (Input.Value != "velocity")
        {
            inputL = GestureTracker.Instance.GetInputTracker(Input.Value, XRNode.LeftHand);
            inputL.OnPressed += OnActivate;
            inputL.OnReleased += OnDeactivate;

            inputR = GestureTracker.Instance.GetInputTracker(Input.Value, XRNode.RightHand);
            inputR.OnPressed += OnActivate;
            inputR.OnReleased += OnDeactivate;
            isVelocity = false;
        }
        else
        {
            isVelocity = true;
        }
    }

    private void Unsub()
    {
        if (inputL != null)
        {
            inputL.OnPressed -= OnActivate;
            inputL.OnReleased -= OnDeactivate;
        }

        if (inputR != null)
        {
            inputR.OnPressed -= OnActivate;
            inputR.OnReleased -= OnDeactivate;
        }
    }

    public static void BindConfigEntries()
    {
        try
        {
            Sticky = Plugin.configFile.Bind(
                DisplayName,
                "sticky",
                false,
                "Whether or not your hands stick to the platforms"
            );

            Scale = Plugin.configFile.Bind(
                DisplayName,
                "size",
                5,
                "The size of the platforms"
            );

            Input = Plugin.configFile.Bind(
                DisplayName,
                "input",
                "grip",
                new ConfigDescription(
                    "Which button you press to activate the platform",
                    new AcceptableValueList<string>("grip", "trigger", "stick", "a/x", "b/y", "velocity")
                )
            );

            Model = Plugin.configFile.Bind(
                DisplayName,
                "model",
                "cloud",
                new ConfigDescription(
                    "Which button you press to activate the platform",
                    new AcceptableValueList<string>("cloud", "storm cloud", "doug", "ice", "invisible")
                )
            );
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    public override string GetDisplayName()
    {
        return "Platforms";
    }

    public override string Tutorial()
    {
        return $"Press [{Input.Value}] to spawn a platform you can stand on. " +
               $"Release [{Input.Value}] to disable it.";
    }
}

public class NetworkedPlatformsHandler : MonoBehaviour
{
    public GameObject? platformLeft, platformRight;
    public NetworkedPlayer? networkedPlayer;

    private void Start()
    {
        try
        {
            networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
            Logging.Debug("Networked player", networkedPlayer.owner.NickName, "turned on platforms");
            platformLeft = Instantiate(Platforms.platformPrefab);
            platformRight = Instantiate(Platforms.platformPrefab);
            SetupPlatform(platformLeft);
            SetupPlatform(platformRight);
            platformLeft.name = networkedPlayer.owner.NickName + "'s Left Platform";
            platformRight.name = networkedPlayer.owner.NickName + "'s Right Platform";
            networkedPlayer.OnGripPressed += OnGripPressed;
            networkedPlayer.OnGripReleased += OnGripReleased;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void OnDestroy()
    {
        platformLeft?.Obliterate();
        platformRight?.Obliterate();
        if (networkedPlayer != null)
        {
            networkedPlayer.OnGripPressed -= OnGripPressed;
            networkedPlayer.OnGripReleased -= OnGripReleased;
        }
    }

    private void SetupPlatform(GameObject platform)
    {
        try
        {
            platform.SetActive(false);
            var rs = platform.AddComponent<RoomSpecific>();
            rs.Owner = networkedPlayer?.owner;
            foreach (Transform child in platform.transform)
                if (!child.name.Contains("cloud"))
                {
                    child.gameObject.Obliterate();
                }
                else
                {
                    child.GetComponent<Collider>()?.Obliterate();
                    child.GetComponent<ParticleSystem>()?.Obliterate();
                }
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void OnGripPressed(NetworkedPlayer player, bool isLeft)
    {
        if (isLeft)
        {
            var leftHand = networkedPlayer.rig.leftHandTransform;
            platformLeft.SetActive(true);
            platformLeft.transform.position = leftHand.TransformPoint(new Vector3(-12, 18, -10) / 200f);
            platformLeft.transform.rotation = leftHand.transform.rotation * Quaternion.Euler(215, 0, -15);
            platformLeft.transform.localScale = Vector3.one * networkedPlayer.rig.scaleFactor;
        }
        else
        {
            var rightHand = networkedPlayer.rig.rightHandTransform;
            platformRight.SetActive(true);
            platformRight.transform.localPosition = rightHand.TransformPoint(new Vector3(12, 18, 10) / 200f);
            platformRight.transform.localRotation = rightHand.transform.rotation * Quaternion.Euler(-45, -25, -190);
            platformLeft.transform.localScale = Vector3.one * networkedPlayer.rig.scaleFactor;
        }
    }

    private void OnGripReleased(NetworkedPlayer player, bool isLeft)
    {
        if (isLeft)
            platformLeft.SetActive(false);
        else
            platformRight.SetActive(false);
    }
}