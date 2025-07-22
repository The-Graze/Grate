using System;
using BepInEx.Configuration;
using GorillaLocomotion.Climbing;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Tools;
using UnityEngine;
using UnityEngine.XR;

namespace Grate.Modules.Movement;

public class NailGun : GrateModule
{
    public static readonly string DisplayName = "Nail Gun";
    public static GameObject launcherPrefab, nailPrefab;

    public static ConfigEntry<int> MaxNailGuns;
    public static ConfigEntry<string> LauncherHand;
    public static ConfigEntry<int> GravityMultiplier;
    public GameObject launcher;
    public GameObject[] nails = new GameObject[0];

    private AudioSource audioFire;
    private GameObject barrel;
    private XRNode hand;

    private int nextNail;

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            if (!launcherPrefab)
            {
                launcherPrefab = Plugin.AssetBundle.LoadAsset<GameObject>("Nail Gun");
                nailPrefab = Plugin.AssetBundle.LoadAsset<GameObject>("Nail");
            }

            launcher = Instantiate(launcherPrefab);
            audioFire = launcher.GetComponent<AudioSource>();
            barrel = launcher.transform.Find("Barrel").gameObject;

            ReloadConfiguration();
            HideLauncher();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void ShowLauncher(InputTracker _)
    {
        launcher.GetComponent<MeshRenderer>().enabled = true;
        GestureTracker.Instance.HapticPulse(hand == XRNode.LeftHand);
    }

    private void HideLauncher()
    {
        launcher.GetComponent<MeshRenderer>().enabled = false;
        ;
    }

    private void Fire(InputTracker _)
    {
        if (!launcher.activeSelf) return;
        audioFire.Play();
        try
        {
            nails[nextNail]?.Obliterate();
            nails[nextNail] = MakeNail();
            nextNail = MathExtensions.Wrap(nextNail + 1, 0, nails.Length);
            GestureTracker.Instance.HapticPulse(hand == XRNode.LeftHand, 1, .25f);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }

        HideLauncher();
    }

    private GameObject MakeNail()
    {
        try
        {
            var nail = Instantiate(nailPrefab);
            var end = GetEndpoint(barrel.transform.position, barrel.transform.forward);
            if (!end.HasValue) return null;
            nail.transform.position = end.Value;
            nail.transform.rotation = barrel.transform.rotation;
            nail.AddComponent<GorillaClimbable>();
            return nail;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }

        return null;
    }

    private Vector3? GetEndpoint(Vector3 origin, Vector3 forward)
    {
        var ray = new Ray(origin, forward);
        RaycastHit hit;
        UnityEngine.Physics.Raycast(ray, out hit, Mathf.Infinity, Teleport.layerMask);
        if (!hit.collider) return null; //if it hits nothing, return null
        return hit.point;
    }

    protected override void Cleanup()
    {
        if (!MenuController.Instance.Built) return;
        UnsubscribeFromEvents();
        launcher?.Obliterate();
        if (nails is null) return;
        foreach (var nail in nails)
            nail?.Obliterate();
    }

    protected override void ReloadConfiguration()
    {
        ResizeArray(MaxNailGuns.Value * 4);
        UnsubscribeFromEvents();

        hand = LauncherHand.Value == "left"
            ? XRNode.LeftHand
            : XRNode.RightHand;

        Parent();

        var grip = GestureTracker.Instance.GetInputTracker("grip", hand);
        var trigger = GestureTracker.Instance.GetInputTracker("trigger", hand);

        trigger.OnPressed += ShowLauncher;
        trigger.OnReleased += Fire;
    }

    public void ResizeArray(int newLength)
    {
        if (newLength < 0)
        {
            Logging.Warning("Cannot resize array to a negative length.");
            return;
        }

        // Check if the new length is smaller than the current length
        if (newLength < nails.Length)
            for (var i = newLength; i < nails.Length; i++)
                nails[i]?.Obliterate();

        if (nextNail >= nails.Length)
            nextNail = 0;

        // Resize the array
        Array.Resize(ref nails, newLength);
    }

    private void Parent()
    {
        var parent = GestureTracker.Instance.rightHand.transform;
        float x = -1;
        if (hand == XRNode.LeftHand)
        {
            parent = GestureTracker.Instance.leftHand.transform;
            x = 1;
        }

        launcher.transform.SetParent(parent, true);
        launcher.transform.localPosition = new Vector3(0.4782f * x, 0.1f, 0.4f);
        launcher.transform.localRotation = Quaternion.Euler(20, 0, 0);
        launcher.transform.localScale = Vector3.one * 18;
    }

    private void UnsubscribeFromEvents()
    {
        if (!GestureTracker.Instance) return;
        var trigger = GestureTracker.Instance.GetInputTracker("trigger", hand);
        trigger.OnPressed -= ShowLauncher;
        trigger.OnReleased -= Fire;
    }

    public static void BindConfigEntries()
    {
        MaxNailGuns = Plugin.ConfigFile.Bind(
            DisplayName,
            "max nails",
            5,
            "Maximum number of nails that can exist at one time (multiplied by 4)"
        );

        LauncherHand = Plugin.ConfigFile.Bind(
            DisplayName,
            "nailgun hand",
            "left",
            new ConfigDescription(
                "Which hand holds the nail gun",
                new AcceptableValueList<string>("left", "right")
            )
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        var h = LauncherHand.Value.Substring(0, 1).ToUpper() + LauncherHand.Value.Substring(1);
        return $"Hold [{h} Trigger] to summon the nailgun. Release [{h} Trigger] to fire a climbable nail. " +
               $"Grip the nail to climb it.";
    }
}