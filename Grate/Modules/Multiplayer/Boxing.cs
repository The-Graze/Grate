using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Configuration;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Networking;
using Grate.Tools;
using UnityEngine;
using NetworkPlayer = NetPlayer;
using Player = GorillaLocomotion.GTPlayer;
using Random = UnityEngine.Random;

namespace Grate.Modules.Multiplayer;

public class BoxingGlove : MonoBehaviour
{
    public static int uuid;
    public VRRig rig;
    public AudioSource punchSound;
    public GorillaVelocityEstimator velocity;

    private void Start()
    {
        punchSound = GetComponent<AudioSource>();
        velocity = gameObject.AddComponent<GorillaVelocityEstimator>();
    }
}

public class Boxing : GrateModule
{
    public static readonly string DisplayName = "Boxing";

    public static ConfigEntry<int> PunchForce;
    public static ConfigEntry<bool> BuffMonke;
    public float forceMultiplier = 5000;
    private readonly List<VRRig> glovedRigs = new();
    private readonly List<BoxingGlove> gloves = new();

    private float lastPunch;
    private Collider punchCollider;

    private void FixedUpdate()
    {
        //if (Time.frameCount % 300 == 0) CreateGloves();
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            ReloadConfiguration();
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "GratePunchDetector";
            capsule.transform.SetParent(Player.Instance.bodyCollider.transform, false);
            capsule.layer = GrateInteractor.InteractionLayer;
            capsule.GetComponent<MeshRenderer>().enabled = false;

            punchCollider = capsule.GetComponent<Collider>();
            punchCollider.isTrigger = true;
            punchCollider.transform.localScale = new Vector3(.5f, .35f, .5f);
            punchCollider.transform.localPosition += new Vector3(0, .3f, 0);

            var observer = capsule.AddComponent<CollisionObserver>();
            observer.OnTriggerEntered += (obj, collider) =>
            {
                if (collider.GetComponentInParent<BoxingGlove>() is BoxingGlove glove) DoPunch(glove);
            };
            NetworkPropertyHandler.Instance.OnPlayerJoined += OnPlayerJoined;
            CreateGloves();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    protected override void OnDisable()
    {
        foreach (var g in Resources.FindObjectsOfTypeAll<BoxingGlove>())
        {
            if (gloves.Contains(g)) gloves.Remove(g);
            g.gameObject.Obliterate();
        }

        base.OnDisable();
    }

    protected override void Cleanup()
    {
        punchCollider?.gameObject?.Obliterate();
        glovedRigs.Clear();
        if (NetworkPropertyHandler.Instance is NetworkPropertyHandler nph) nph.OnPlayerJoined -= OnPlayerJoined;
        StartCoroutine(DelGloves());
    }

    public IEnumerator DelGloves()
    {
        /*foreach (var g in Resources.FindObjectsOfTypeAll<BoxingGlove>())
        {
            g.Obliterate();
        }*/
        foreach (var g in gloves) g.Obliterate();
        gloves.Clear();
        yield return new WaitForEndOfFrame();
    }

    private void OnPlayerJoined(NetworkPlayer player)
    {
        GiveGlovesTo(player.Rig());
    }

    private void CreateGloves()
    {
        foreach (var rig in GorillaParent.instance.vrrigs)
            try
            {
                if (rig != GorillaTagger.Instance.offlineVRRig && rig != GorillaTagger.Instance.myVRRig &&
                    !glovedRigs.Contains(rig) &&
                    rig.OwningNetPlayer != GorillaTagger.Instance.myVRRig.Owner) GiveGlovesTo(rig);
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
    }

    private void GiveGlovesTo(VRRig rig)
    {
        glovedRigs.Add(rig);
        var lefty = CreateGlove(rig.leftHandTransform);
        lefty.rig = rig;
        gloves.Add(lefty);
        var righty = CreateGlove(rig.rightHandTransform, false);
        righty.rig = rig;
        gloves.Add(righty);
        Logging.Debug("Gave gloves to", rig.OwningNetPlayer.NickName);
    }

    private BoxingGlove CreateGlove(Transform parent, bool isLeft = true)
    {
        var glove = Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>("Boxing Glove"));
        var side = isLeft ? "Left" : "Right";
        glove.name = $"Boxing Glove ({side})";
        glove.transform.SetParent(parent, false);
        float x = isLeft ? 1 : -1;
        glove.transform.localScale = new Vector3(x, 1, 1);
        glove.layer = GrateInteractor.InteractionLayer;
        foreach (Transform child in glove.transform)
            child.gameObject.layer = GrateInteractor.InteractionLayer;
        return glove.AddComponent<BoxingGlove>();
    }

    private void DoPunch(BoxingGlove glove)
    {
        if (Time.time - lastPunch < 1) return;
        var force = glove.velocity.linearVelocity;
        if (force.magnitude < .5f * Player.Instance.scale) return;
        force.Normalize();
        force *= forceMultiplier * Buffness();
        Player.Instance.bodyCollider.attachedRigidbody.velocity += force;
        lastPunch = Time.time;
        GestureTracker.Instance.HapticPulse(false);
        GestureTracker.Instance.HapticPulse(true);
        glove.punchSound.pitch = Random.Range(.8f, 1.2f);
        glove.punchSound.Play();
    }

    public int Buffness()
    {
        if (BuffMonke.Value) return 100;

        return 1;
    }

    protected override void ReloadConfiguration()
    {
        forceMultiplier = PunchForce.Value * 5;
    }

    public static void BindConfigEntries()
    {
        Logging.Debug("Binding", DisplayName, "to config");
        PunchForce = Plugin.ConfigFile.Bind(
            DisplayName,
            "punch force",
            5,
            "How much force will be applied to you when you get punched"
        );

        BuffMonke = Plugin.ConfigFile.Bind(
            DisplayName,
            "Buff monke",
            false,
            "WEEEEEEEEEEEE"
        );
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Effect: Other players can punch you around.";
    }
}