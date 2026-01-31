using UnityEngine;
using UnityEngine;
﻿using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Modules.Physics;
using Grate.Patches;
using Grate.Tools;
using UnityEngine.XR;
using Random = Unity.Mathematics.Random;

namespace Grate.Modules.Teleportation;

public class Checkpoint : GrateModule
{
    public static readonly string DisplayName = "Checkpoint";
    public static Checkpoint Instance;

    public static ConfigEntry<int> ChargeTime;
    private LineRenderer bananaLine;

    private Transform checkpointMarker;
    private Vector3 checkpointPosition, checkpointMarkerPosition;
    private GameObject checkpointPrefab, bananaLinePrefab;
    private Vector3 checkpointRotation;

    private List<GorillaTriggerBox> markedTriggers;
    private bool pointSet;

    private void Awake()
    {
        Instance = this;
    }

    protected override void Start()
    {
        try
        {
            base.Start();
            checkpointPrefab = Plugin.AssetBundle.LoadAsset<GameObject>("Checkpoint Banana");
            checkpointPrefab.gameObject.SetActive(false);
            bananaLinePrefab = Plugin.AssetBundle.LoadAsset<GameObject>("Banana Line");
            bananaLinePrefab.gameObject.SetActive(false);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void FixedUpdate()
    {
        checkpointMarker.Rotate(Vector3.up, 90 * Time.fixedDeltaTime, Space.World);
    }

    protected override void OnEnable()
    {
        try
        {
            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            checkpointMarker = Instantiate(checkpointPrefab).transform;
            checkpointMarker.position = checkpointPosition;
            checkpointMarker.gameObject.SetActive(pointSet);
            bananaLine = Instantiate(bananaLinePrefab).GetComponent<LineRenderer>();
            markedTriggers = new List<GorillaTriggerBox>();
            //foreach (var triggerBox in FindObjectsOfType<GorillaTriggerBox>())
            //{
            //    if (triggerBox?.gameObject?.GetComponent<CollisionObserver>()) continue;
            //    var observer = triggerBox.gameObject.AddComponent<CollisionObserver>();
            //    // Sometimes you just can't add a collision observer for some reason. If this happens, give up.
            //    if (!triggerBox?.gameObject?.GetComponent<CollisionObserver>()) continue;

            //    observer.OnTriggerStayed += (box, collider) =>
            //    {
            //        if (collider == Player.Instance.bodyCollider)
            //            ClearCheckpoint();
            //    };
            //    markedTriggers.Add(triggerBox);
            //}
            GestureTracker.Instance.leftTrigger.OnPressed += Triggered;
            GestureTracker.Instance.rightTrigger.OnPressed += Triggered;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void Triggered(InputTracker input)
    {
        if (!enabled) return;
        if (input.node == XRNode.LeftHand)
            StartCoroutine(GrowBananas());
        else if (pointSet)
            StartCoroutine(GoBananas());
    }

    // Creates the checkpoint
    private IEnumerator GrowBananas()
    {
        checkpointMarker.gameObject.SetActive(true);
        var startTime = Time.time;
        while (GestureTracker.Instance.leftTrigger.pressed && !NoClip.active)
        {
            var chargeScale = MathExtensions.Map(ChargeTime.Value, 0, 10, 0f, 1f);
            var scale = Mathf.Lerp(0, GTPlayer.Instance.scale, (Time.time - startTime) / chargeScale);
            checkpointMarker.position =
                VRRig.LocalRig.leftHand.rigTarget.position + Vector3.up * .15f * GTPlayer.Instance.scale;
            checkpointMarker.localScale = Vector3.one * scale;
            if (Mathf.Abs(scale - GTPlayer.Instance.scale) < .01f)
            {
                GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(UnityEngine.Random.Range(40, 56), false, 0.1f);
                GestureTracker.Instance.HapticPulse(true);
                checkpointPosition = VRRig.LocalRig.leftHand.rigTarget.position +
                                     Vector3.up * .15f * GTPlayer.Instance.scale;
                checkpointRotation = GTPlayer.Instance.headCollider.transform.eulerAngles;
                pointSet = true;
                checkpointMarker.localScale = Vector3.one * GTPlayer.Instance.scale;
                checkpointMarkerPosition = checkpointMarker.position;
                break;
            }

            yield return new WaitForFixedUpdate();
        }

        if (!pointSet)
        {
            checkpointMarker.localScale = Vector3.zero;
            checkpointMarker.gameObject.SetActive(pointSet);
        }
        else
        {
            checkpointMarker.position = checkpointMarkerPosition;
            checkpointMarker.localScale = Vector3.one * GTPlayer.Instance.scale;
        }
    }

    // Warps the player to the checkpoint
    private IEnumerator GoBananas()
    {
        bananaLine.gameObject.SetActive(true);
        var startTime = Time.time;
        Vector3 startPos, endPos;
        while (GestureTracker.Instance.rightTrigger.pressed && pointSet)
        {
            startPos = GTPlayer.Instance.rightHand.controllerTransform.position;
            bananaLine.SetPosition(1, startPos);
            var chargeScale = MathExtensions.Map(ChargeTime.Value, 0, 10, 0f, 1f);
            endPos = Vector3.Lerp(startPos, checkpointMarker.transform.position, (Time.time - startTime) / chargeScale);
            bananaLine.SetPosition(0, endPos);
            bananaLine.startWidth = bananaLine.endWidth = GTPlayer.Instance.scale * .1f;
            bananaLine.material.mainTextureScale = new Vector2(GTPlayer.Instance.scale, 1);
            if (Vector3.Distance(endPos, checkpointMarker.transform.position) < .01f)
            {
                TeleportPatch.TeleportPlayer(checkpointPosition, checkpointRotation.y);
                break;
            }

            yield return new WaitForFixedUpdate();
        }

        bananaLine.gameObject.SetActive(false);
    }


    public void ClearCheckpoint()
    {
        if (!pointSet) return;
        GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(68, false, 1f);
        checkpointMarker.gameObject.SetActive(false);
        pointSet = false;
        bananaLine.gameObject.SetActive(false);
    }

    protected override void Cleanup()
    {
        if (!MenuController.Instance.Built) return;
        if (bananaLine != null)
        {
            bananaLine?.gameObject.Obliterate();
            checkpointMarker?.gameObject.Obliterate();
        }

        if (GestureTracker.Instance)
        {
            GestureTracker.Instance.leftTrigger.OnPressed -= Triggered;
            GestureTracker.Instance.rightTrigger.OnPressed -= Triggered;
        }

        if (markedTriggers is null) return;
        foreach (var triggerBox in markedTriggers) triggerBox.GetComponent<CollisionObserver>()?.Obliterate();
    }

    public static void BindConfigEntries()
    {
        ChargeTime = Plugin.ConfigFile.Bind(
            DisplayName,
            "charge time",
            5,
            "How long it takes to charge the teleport"
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Hold [Left Trigger] to spawn a checkpoint. Hold [Right Trigger] to return to it.";
    }
}