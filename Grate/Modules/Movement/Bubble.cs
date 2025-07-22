using System;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using Grate.Patches;
using Grate.Tools;
using UnityEngine;
using NetworkPlayer = NetPlayer;

namespace Grate.Modules.Movement;

public class BubbleMarker : MonoBehaviour
{
    private GameObject bubble;

    private void Start()
    {
        bubble = Instantiate(Bubble.bubblePrefab);
        bubble.transform.SetParent(transform, false);
        bubble.transform.localPosition = new Vector3(0, -.1f, 0);
        bubble.gameObject.layer = GrateInteractor.InteractionLayer;
    }

    private void OnDestroy()
    {
        Destroy(bubble);
    }
}

public class Bubble : GrateModule
{
    public static readonly string DisplayName = "Bubble";
    public static GameObject bubblePrefab;
    public static Vector3 bubbleOffset = new(0, .15f, 0);

    public static ConfigEntry<int> BubbleSize;
    public static ConfigEntry<int> BubbleSpeed;
    public GameObject bubble;
    public GameObject colliderObject;
    public Vector3 targetPosition;
    private readonly float cooldown = .1f;

    private readonly float margin = .1f;


    private float baseDrag;
    private float colliderScale = 1;
    private float lastTouchLeft, lastTouchRight;

    private bool leftWasTouching, rightWasTouching;

    private Rigidbody rb;

    private void Awake()
    {
        if (!bubblePrefab) bubblePrefab = Plugin.AssetBundle.LoadAsset<GameObject>("BubbleP");
        NetworkPropertyHandler.Instance.OnPlayerModStatusChanged += OnPlayerModStatusChanged;
        VRRigCachePatches.OnRigCached += OnRigCached;
    }

    private void FixedUpdate()
    {
        if (!rb)
            rb = GTPlayer.Instance.bodyCollider.attachedRigidbody;
        rb.AddForce(-UnityEngine.Physics.gravity * rb.mass * GTPlayer.Instance.scale);
        bubble.transform.localScale = Vector3.one * GTPlayer.Instance.scale * .75f;
    }

    private void LateUpdate()
    {
        if (bubble != null)
        {
            bubble.transform.position = GTPlayer.Instance.headCollider.transform.position;
            bubble.transform.position -= bubbleOffset * GTPlayer.Instance.scale;

            Vector3 leftPos = GestureTracker.Instance.leftHand.transform.position,
                rightPos = GestureTracker.Instance.rightHand.transform.position;

            if (Touching(leftPos))
            {
                if (!leftWasTouching && Time.time - lastTouchLeft > cooldown)
                {
                    OnTouch(leftPos, true);
                    lastTouchLeft = Time.time;
                }

                leftWasTouching = true;
            }
            else
            {
                leftWasTouching = false;
            }

            if (Touching(rightPos))
            {
                if (!rightWasTouching && Time.time - lastTouchRight > cooldown)
                {
                    OnTouch(rightPos, false);
                    lastTouchRight = Time.time;
                }

                rightWasTouching = true;
            }
            else
            {
                rightWasTouching = false;
            }
        }
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            ReloadConfiguration();
            bubble = Instantiate(bubblePrefab);
            bubble.AddComponent<GorillaSurfaceOverride>().overrideIndex = 110;
            bubble.GetComponent<Collider>().enabled = false;
            rb = GTPlayer.Instance.bodyCollider.attachedRigidbody;
            baseDrag = rb.drag;
            rb.drag = 1;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void OnRigCached(NetPlayer player, VRRig rig)
    {
        rig?.gameObject?.GetComponent<BubbleMarker>()?.Obliterate();
    }

    private void OnPlayerModStatusChanged(NetworkPlayer player, string mod, bool enabled)
    {
        if (mod != DisplayName) return;
        if (enabled)
            player.Rig().gameObject.GetOrAddComponent<BubbleMarker>();
        else
            Destroy(player.Rig().gameObject.GetComponent<BubbleMarker>());
    }

    private bool Touching(Vector3 position)
    {
        var radius = GTPlayer.Instance.scale * colliderScale;
        var d = Vector3.Distance(position, bubble.transform.position);
        var m = margin * GTPlayer.Instance.scale;
        return d > radius - m && d < radius + m;
    }

    private void OnTouch(Vector3 position, bool left)
    {
        Sounds.Play(110);
        position -= bubble.transform.position;
        GestureTracker.Instance.HapticPulse(left);
        GTPlayer.Instance.AddForce(position.normalized * GTPlayer.Instance.scale * BubbleSpeed.Value / 5);
    }

    protected override void Cleanup()
    {
        if (bubble)
            Sounds.Play(84, 2);
        bubble?.Obliterate();
        if (rb)
            rb.drag = baseDrag;
    }

    protected override void ReloadConfiguration()
    {
        colliderScale = MathExtensions.Map(BubbleSize.Value, 0, 10, .45f, .65f);
    }

    public static void BindConfigEntries()
    {
        BubbleSize = Plugin.ConfigFile.Bind(
            DisplayName,
            "bubble size",
            5,
            "How far you have to reach to hit the bubble"
        );
        BubbleSpeed = Plugin.ConfigFile.Bind(
            DisplayName,
            "bubble speed",
            5,
            "How fast the bubble moves when you push it"
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Creates a bubble around you so you can float. " +
               "Tap the side that you want to move towards to move.";
    }
}