using System.Collections.Generic;
using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using UnityEngine;
using UnityEngine.XR;

namespace Grate.Modules.Movement;

internal class Frozone : GrateModule
{
    public static GameObject IcePrefab;
    public static Vector3 LhandOffset = Vector3.down * 0.05f;
    public static Vector3 RhandOffset = Vector3.down * 0.107f;
    private readonly List<GameObject> prevLIce = new();
    private readonly List<GameObject> prevRIce = new();

    private InputTracker inputL, inputR;

    private bool leftPress, rightPress;
    private Transform leftHandTransform => VRRig.LocalRig.leftHandTransform;
    private Transform rightHandTransform => VRRig.LocalRig.rightHandTransform;

    protected override void Start()
    {
        base.Start();
        IcePrefab = Plugin.assetBundle.LoadAsset<GameObject>("Ice");
        IcePrefab.GetComponent<BoxCollider>().enabled = true;
        IcePrefab.AddComponent<GorillaSurfaceOverride>().overrideIndex = 59;
    }

    private void FixedUpdate()
    {
        if (leftPress)
        {
            if (prevLIce.Count > 19)
            {
                var ice = prevLIce[0];
                prevLIce.RemoveAt(0);
                ice.SetActive(true);
                ice.transform.position = leftHandTransform.position + LhandOffset;
                ice.transform.rotation = leftHandTransform.rotation;
                prevLIce.Add(ice);
            }
            else
            {
                var ice = Instantiate(IcePrefab);
                ice.AddComponent<RoomSpecific>();
                ice.transform.position = leftHandTransform.position + LhandOffset;
                ice.transform.rotation = leftHandTransform.rotation;
                prevLIce.Add(ice);
            }
        }
        else
        {
            foreach (var ice in prevLIce) ice.SetActive(false);
        }

        if (rightPress)
        {
            if (prevRIce.Count >= 20)
            {
                var ice = prevRIce[0];
                prevRIce.RemoveAt(0);
                ice.SetActive(true);

                ice.transform.position = rightHandTransform.position + RhandOffset;
                ice.transform.rotation = rightHandTransform.rotation;
                prevRIce.Add(ice);
            }
            else
            {
                var ice = Instantiate(IcePrefab);
                ice.AddComponent<RoomSpecific>();
                ice.transform.position = rightHandTransform.position + RhandOffset;
                ice.transform.rotation = rightHandTransform.rotation;
                prevRIce.Add(ice);
            }
        }
        else
        {
            foreach (var ice in prevRIce) ice.SetActive(false);
        }
    }


    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        inputL = GestureTracker.Instance.GetInputTracker("grip", XRNode.LeftHand);
        inputL.OnPressed += OnActivate;
        inputL.OnReleased += OnDeactivate;

        inputR = GestureTracker.Instance.GetInputTracker("grip", XRNode.RightHand);
        inputR.OnPressed += OnActivate;
        inputR.OnReleased += OnDeactivate;
        Plugin.menuController.GetComponent<Platforms>().button.AddBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }

    public override string GetDisplayName()
    {
        return "Frozone";
    }

    public override string Tutorial()
    {
        return "Like Platforms but you slide!";
    }

    private void OnActivate(InputTracker tracker)
    {
        if (tracker.node == XRNode.LeftHand) leftPress = true;
        if (tracker.node == XRNode.RightHand) rightPress = true;
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

    private void OnDeactivate(InputTracker tracker)
    {
        if (tracker.node == XRNode.LeftHand) leftPress = false;
        if (tracker.node == XRNode.RightHand) rightPress = false;
    }

    protected override void Cleanup()
    {
        Unsub();
        Plugin.menuController.GetComponent<Platforms>().button.RemoveBlocker(ButtonController.Blocker.MOD_INCOMPAT);
    }
}