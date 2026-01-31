using UnityEngine;
using UnityEngine;
﻿using System;
using System.Collections;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Patches;
using Grate.Tools;
using Random = Unity.Mathematics.Random;

namespace Grate.Modules;

public class Teleport : GrateModule
{
    public static readonly string DisplayName = "Teleport";
    public static readonly int layerMask = LayerMask.GetMask("Default", "Gorilla Object");

    public static ConfigEntry<int> ChargeTime;
    private bool isTeleporting;
    private DebugPoly poly;

    private Transform teleportMarker, window;

    private void FixedUpdate()
    {
        teleportMarker.Rotate(Vector3.up, 90 * Time.fixedDeltaTime, Space.World);
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            teleportMarker = Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>("Checkpoint Banana")).transform;
            teleportMarker.gameObject.SetActive(false);
            window = new GameObject("Teleport Window").transform;
            poly = window.gameObject.AddComponent<DebugPoly>();
            GestureTracker.Instance.OnIlluminati += OnIlluminati;
            Application.onBeforeRender += RenderTriangle;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void OnIlluminati()
    {
        if (enabled)
            if (!isTeleporting)
                StartCoroutine(GrowBananas());
    }

    private IEnumerator GrowBananas()
    {
        isTeleporting = true;
        teleportMarker.gameObject.SetActive(true);
        var startTime = Time.time;
        Transform
            leftHand = GestureTracker.Instance.leftPalmInteractor.transform,
            rightHand = GestureTracker.Instance.rightPalmInteractor.transform;
        var playedSound = false;
        var player = GTPlayer.Instance;
        while (GestureTracker.Instance.isIlluminatiing)
        {
            window.transform.position = (leftHand.position + rightHand.position) / 2;
            if (!TriangleInRange())
            {
                teleportMarker.position = Vector3.up * 100000;
                startTime = Time.time;
                yield return new WaitForEndOfFrame();
                continue;
            }

            RaycastHit hit;
            var forward = GestureTracker.Instance.headVectors.pointerDirection;
            var ray = new Ray(
                player.headCollider.transform.position,
                forward
            );
            UnityEngine.Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask);
            if (!hit.transform)
            {
                startTime = Time.time;
                teleportMarker.position = Vector3.up * 100000;
                if (playedSound)
                {
                    GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(98, false, .1f);
                    playedSound = false;
                }

                yield return new WaitForEndOfFrame();
                continue;
            }

            if (!playedSound)
            {
                GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(UnityEngine.Random.Range(40, 56), false, 0.1f);
                playedSound = true;
            }

            var chargeScale = MathExtensions.Map(ChargeTime.Value, 0, 10, .25f, 1.5f);
            var t = Mathf.Lerp(0, 1, (Time.time - startTime) / chargeScale);
            teleportMarker.position = hit.point - forward * player.scale;
            teleportMarker.localScale = Vector3.one * GTPlayer.Instance.scale * t;
            if (t >= 1)
            {
                TeleportPatch.TeleportPlayer(teleportMarker.position, teleportMarker.rotation.y);
                break;
            }

            yield return new WaitForFixedUpdate();
        }

        teleportMarker.gameObject.SetActive(false);
        isTeleporting = false;
        poly.renderer.enabled = false;
    }


    private bool TriangleInRange()
    {
        return true;
    }

    private void RenderTriangle()
    {
        if (!GestureTracker.Instance.isIlluminatiing) return;
        poly.renderer.enabled = true;
        var gt = GestureTracker.Instance;
        var a = gt.leftThumbTransform.position - gt.leftThumbTransform.up * .03f + gt.leftThumbTransform.right * -.02f;
        var b = gt.rightThumbTransform.position - gt.rightThumbTransform.up * .03f +
                gt.rightThumbTransform.right * .02f;
        var c = (gt.rightPointerTransform.position + gt.leftPointerTransform.position) / 2f;

        a = poly.transform.InverseTransformPoint(a);
        b = poly.transform.InverseTransformPoint(b);
        c = poly.transform.InverseTransformPoint(c);

        poly.vertices = new[] { a, b, c };
    }

    protected override void Cleanup()
    {
        if (!MenuController.Instance.Built) return;
        Application.onBeforeRender -= RenderTriangle;
        teleportMarker?.gameObject?.Obliterate();
        window?.gameObject?.Obliterate();
        isTeleporting = false;
        if (GestureTracker.Instance is null) return;
        GestureTracker.Instance.OnIlluminati -= OnIlluminati;
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
        return
            "To teleport, make a triangle with your thumbs and index fingers and" +
            "look at where you want to teleport.";
    }
}