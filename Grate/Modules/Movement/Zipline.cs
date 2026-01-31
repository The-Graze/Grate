using UnityEngine;
using UnityEngine;
﻿using System;
using BepInEx.Configuration;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Gameplay;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Tools;
using HarmonyLib;
using UnityEngine.XR;

namespace Grate.Modules.Movement;

public class Zipline : GrateModule
{
    public static readonly string DisplayName = "Zipline";
    public static GameObject launcherPrefab, ziplinePrefab;

    public static AudioClip ziplineAudioLoop;

    public static ConfigEntry<int> MaxZiplines;
    public static ConfigEntry<string> LauncherHand;
    public static ConfigEntry<int> GravityMultiplier;
    private AudioSource audioSlide, audioFire;
    private GorillaClimbable climbable;
    private Transform climbOffsetHelper;
    private GameObject gunStartHook, gunEndHook;
    private XRNode hand;
    public GameObject launcher;


    private int nextZipline;
    private GorillaZiplineSettings settings;
    private ParticleSystem[] smokeSystems;
    public GameObject[] ziplines = new GameObject[0];

    private void Awake()
    {
        settings = ScriptableObject.CreateInstance<GorillaZiplineSettings>();
        settings.gravityMulti = 0;
        settings.maxFriction = 0;
        settings.friction = 0;
        settings.maxSpeed *= 2;
    }


    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            if (!launcherPrefab)
            {
                launcherPrefab = Plugin.AssetBundle.LoadAsset<GameObject>("Zipline Launcher");
                ziplinePrefab = Plugin.AssetBundle.LoadAsset<GameObject>("Zipline Rope");
                ziplineAudioLoop = Plugin.AssetBundle.LoadAsset<AudioClip>("Zipline Loop");
            }

            launcher = Instantiate(launcherPrefab);
            audioFire = launcher.GetComponent<AudioSource>();
            gunStartHook = launcher.transform.Find("Start Hook").gameObject;
            gunEndHook = launcher.transform.Find("End Hook").gameObject;

            ReloadConfiguration();

            smokeSystems = launcher.GetComponentsInChildren<ParticleSystem>();
            foreach (var system in smokeSystems)
                system.gameObject.SetActive(false);

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
        ResetHooks();
    }

    private void HideLauncher()
    {
        launcher.GetComponent<MeshRenderer>().enabled = false;
        ;
        gunStartHook.SetActive(false);
        gunEndHook.SetActive(false);
        //foreach (var system in smokeSystems)
        //    system.gameObject.SetActive(false);
    }

    private void Fire(InputTracker _)
    {
        if (!launcher.activeSelf) return;
        audioFire.Play();

        GestureTracker.Instance.HapticPulse(hand == XRNode.LeftHand, 1, .25f);
        foreach (var system in smokeSystems)
        {
            system.gameObject.SetActive(true);
            system.Clear();
            system.Play();
        }

        ziplines[nextZipline]?.Obliterate();
        ziplines[nextZipline] = MakeZipline();
        nextZipline = MathExtensions.Wrap(nextZipline + 1, 0, ziplines.Length);
        gunStartHook.SetActive(false);
        gunEndHook.SetActive(false);
        HideLauncher();
    }

    private void ResetHooks()
    {
        if (!launcher.activeSelf) return;
        GestureTracker.Instance.HapticPulse(hand == XRNode.LeftHand);
        gunStartHook.SetActive(true);
        gunEndHook.SetActive(true);
    }

    private GameObject MakeZipline()
    {
        try
        {
            var zipline = Instantiate(ziplinePrefab);
            // Figure out where the ends of the rope will be
            var endpoints = GetEndpoints(gunStartHook.transform.position, gunStartHook.transform.up);
            var start = endpoints[0];
            var end = endpoints[1];
            zipline.transform.position = start;

            var startHook = zipline.transform.Find("Start Hook");
            var endHook = zipline.transform.Find("End Hook");

            startHook.transform.position = start;
            endHook.transform.position = end;
            startHook.localScale *= GTPlayer.Instance.scale;
            endHook.localScale *= GTPlayer.Instance.scale;
            startHook.rotation = gunStartHook.transform.rotation;
            endHook.rotation = gunEndHook.transform.rotation;

            var ropeRenderer = zipline.GetComponent<LineRenderer>();
            ropeRenderer.positionCount = 2;
            ropeRenderer.SetPosition(0, startHook.GetChild(0).position);
            ropeRenderer.SetPosition(1, endHook.GetChild(0).position);
            ropeRenderer.enabled = true;
            ropeRenderer.startWidth = 0.05f * GTPlayer.Instance.scale;
            ropeRenderer.endWidth = 0.05f * GTPlayer.Instance.scale;

            // Set up the segment objects
            MakeSlideHelper(zipline.transform);
            var segments = MakeSegments(start, end);
            segments.parent = zipline.transform;
            segments.localPosition = Vector3.zero;
            // Create the spline which dictates the path you follow
            var spline = zipline.AddComponent<BezierSpline>();
            spline.Reset();
            Traverse.Create(spline).Field("points").SetValue(
                SplinePoints(zipline.transform, start, end)
            );
            //This thing does something important, I don't know what.
            climbOffsetHelper = new GameObject("Climb Offset Helper").transform;
            //Time to put it all together! Create the zipline controller
            var gorillaZipline = zipline.AddComponent<GorillaZipline>();
            //Assign everything to the zipline controller
            climbOffsetHelper.SetParent(zipline.transform, false);
            var traverse = Traverse.Create(gorillaZipline);
            traverse.Field("spline").SetValue(spline);
            traverse.Field("segmentsRoot").SetValue(segments);
            traverse.Field("slideHelper").SetValue(climbable);
            traverse.Field("audioSlide").SetValue(audioSlide);
            traverse.Field("climbOffsetHelper").SetValue(climbOffsetHelper);
            traverse.Field("settings").SetValue(settings);
            var length = (end - start).magnitude;
            traverse.Field("ziplineDistance").SetValue(length);
            traverse.Field("segmentDistance").SetValue(length);

            return zipline;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }

        return null;
    }

    private Vector3[] SplinePoints(Transform parent, Vector3 start, Vector3 end)
    {
        return new[]
        {
            parent.InverseTransformPoint(start),
            parent.InverseTransformPoint(start + (end - start) / 4f),
            parent.InverseTransformPoint(end - (end - start) / 4f),
            parent.InverseTransformPoint(end)
        };
    }

    private Vector3[] GetEndpoints(Vector3 origin, Vector3 forward)
    {
        Vector3 start, end;
        var ray = new Ray(origin, forward);
        RaycastHit hit;

        // Shoot a ray forward
        UnityEngine.Physics.Raycast(ray, out hit, Mathf.Infinity, Teleport.layerMask);
        if (!hit.collider) return null; //if it hits nothing, return null
        end = hit.point;

        // Shoot a ray backward
        ray.direction *= -1;
        UnityEngine.Physics.Raycast(ray, out hit, Mathf.Infinity, Teleport.layerMask);
        if (!hit.collider) return null; //if it hits nothing, return null
        start = hit.point;

        return new[] { start, end };
    }

    private void MakeSlideHelper(Transform parent)
    {
        var slideHelper = new GameObject("SlideHelper");
        slideHelper.transform.SetParent(parent, false);
        slideHelper.AddComponent<GorillaSurfaceOverride>().overrideIndex = 89;
        climbable = slideHelper.AddComponent<GorillaClimbable>();
        climbable.snapX = true;
        climbable.snapY = true;
        climbable.snapZ = true;

        audioSlide = slideHelper.AddComponent<AudioSource>(); // add an audio clip to this somehow
        audioSlide.clip = ziplineAudioLoop;
    }

    private Transform MakeSegments(Vector3 start, Vector3 end)
    {
        var distance = (end - start).magnitude;
        var segments = new GameObject("Segments").transform;
        segments.position = start;

        //for (int i = 0; i < distance; i++)
        //{
        //    Vector3 position = Vector3.Lerp(start, end, i / distance);
        //    GameObject segment = MakeSegment(position, start, end);
        //    segment.transform.SetParent(segments);
        //}

        var position = Vector3.Lerp(start, end, 0.5f);
        var segment = MakeSegment(position, start, end);
        segment.transform.SetParent(segments);

        return segments;
    }

    private GameObject MakeSegment(Vector3 position, Vector3 start, Vector3 end)
    {
        var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.transform.position = position;
        segment.AddComponent<GorillaClimbableRef>().climb = climbable;
        segment.GetComponent<BoxCollider>().isTrigger = true;
        segment.layer = LayerMask.NameToLayer("GorillaInteractable");
        var distance = (end - start).magnitude;
        segment.transform.localScale =
            new Vector3(0.05f * GTPlayer.Instance.scale, 0.05f * GTPlayer.Instance.scale, distance);
        segment.transform.LookAt(end, Vector3.up);
        segment.GetComponent<MeshRenderer>().enabled = false;

        return segment;
    }

    protected override void Cleanup()
    {
        if (!MenuController.Instance.Built) return;
        UnsubscribeFromEvents();
        launcher?.Obliterate();
        gunStartHook?.gameObject?.Obliterate();
        gunEndHook?.gameObject?.Obliterate();
        if (ziplines is null) return;
        foreach (var zipline in ziplines)
            zipline?.Obliterate();
    }

    protected override void ReloadConfiguration()
    {
        settings.gravityMulti = GravityMultiplier.Value / 5f;
        ResizeArray(MaxZiplines.Value);

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
        if (newLength < ziplines.Length)
            for (var i = newLength; i < ziplines.Length; i++)
                ziplines[i]?.Obliterate();

        if (nextZipline >= ziplines.Length)
            nextZipline = 0;

        // Resize the array
        Array.Resize(ref ziplines, newLength);
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
        launcher.transform.localRotation = Quaternion.Euler(20, 0, 180);
        launcher.transform.localScale = Vector3.one * 18;
    }

    private void UnsubscribeFromEvents()
    {
        if (!GestureTracker.Instance) return;
        var grip = GestureTracker.Instance.GetInputTracker("grip", hand);
        var trigger = GestureTracker.Instance.GetInputTracker("trigger", hand);
        trigger.OnPressed -= ShowLauncher;
        trigger.OnReleased -= Fire;
    }

    public static void BindConfigEntries()
    {
        MaxZiplines = Plugin.ConfigFile.Bind(
            DisplayName,
            "max ziplines",
            3,
            "Maximum number of ziplines that can exist at one time"
        );

        LauncherHand = Plugin.ConfigFile.Bind(
            DisplayName,
            "launcher hand",
            "right",
            new ConfigDescription(
                "Which hand holds the launcher",
                new AcceptableValueList<string>("left", "right")
            )
        );

        GravityMultiplier = Plugin.ConfigFile.Bind(
            DisplayName,
            "gravity multiplier",
            5,
            "Gravity multiplier while on the zipline"
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        var h = LauncherHand.Value.Substring(0, 1).ToUpper() + LauncherHand.Value.Substring(1);
        return $"Hold [{h} Trigger] to summon the zipline cannon. Release [{h} Trigger] to fire a zipline.";
    }
}