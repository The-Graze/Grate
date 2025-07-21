using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Interaction;
using Grate.Networking;
using Grate.Patches;
using Grate.Tools;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace Grate.Modules.Physics;

public class Potions : GrateModule
{
    public static readonly string DisplayName = "Potions";
    public static SizeChanger sizeChanger;
    public static Traverse sizeChangerTraverse, minScale, maxScale;
    public static Potions Instance;
    public static bool active;

    // Networking
    public static readonly string playerSizeKey = "GratePlayerSize";
    public static Dictionary<VRRig, SizeChanger> sizeChangers = new();

    public static ConfigEntry<bool> ShowNetworkedSizes, ShowPotions;
    private readonly Vector3 holsterOffset = new(0.15f, -0.15f, 0.15f);
    private GameObject bottlePrefab, shrinkPotion, growPotion;

    private float cachedSize;
    private Transform holsterL, holsterR;
    private Material shrinkMaterial, growMaterial;

    private void Awake()
    {
        try
        {
            Instance = this;
            NetworkPropertyHandler.Instance?.ChangeProperty(playerSizeKey, GTPlayer.Instance.scale);
            bottlePrefab = Plugin.assetBundle.LoadAsset<GameObject>("Potion Bottle");
            shrinkMaterial = Plugin.assetBundle.LoadAsset<Material>("Portal A Material");
            growMaterial = Plugin.assetBundle.LoadAsset<Material>("Portal B Material");
            VRRigCachePatches.OnRigCached += OnRigCached;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void FixedUpdate()
    {
        if (cachedSize == GTPlayer.Instance.scale) return;
        NetworkPropertyHandler.Instance.ChangeProperty(playerSizeKey, GTPlayer.Instance.scale);
        cachedSize = GTPlayer.Instance.scale;
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        NetworkPropertyHandler.Instance.ChangeProperty(playerSizeKey, GTPlayer.Instance.scale);
        base.OnEnable();
        active = false;
        Setup();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (var rig in GorillaParent.instance.vrrigs)
        {
            try
            {
                rig.transform.localScale = Vector3.one;
                rig.ScaleMultiplier = 1;
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }

            ;
        }

        foreach (var manager in FindObjectsOfType<SizeManager>())
        {
            var managerTraverse = Traverse.Create(manager);
            var scaleFromChanger = managerTraverse.Method("ScaleFromChanger");
            var controllingChanger = managerTraverse.Method("ControllingChanger");
            try
            {
                if (manager.myType != SizeManager.SizeChangerType.LocalOffline)
                {
                    var t = manager.targetRig?.transform;
                    if (!t) continue;
                    var scale = scaleFromChanger.GetValue<float>(controllingChanger.GetValue<SizeChanger>(t), t);
                    t.localScale = Vector3.one * scale;
                    manager.targetRig.ScaleMultiplier = scale;
                    NetworkPropertyHandler.Instance?.ChangeProperty(playerSizeKey, GTPlayer.Instance.scale);
                }
                else
                {
                    var t = manager.mainCameraTransform;
                    var player = manager.targetPlayer;
                    var scale = scaleFromChanger.GetValue<float>(controllingChanger.GetValue<SizeChanger>(t), t);
                    player.turnParent.transform.localScale = Vector3.one * scale;
                    player.SetScaleMultiplier(scale);
                }
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }

            ;
        }
    }

    private void OnRigCached(NetPlayer player, VRRig rig)
    {
        try
        {
            rig.transform.localScale = Vector3.one;
            rig.ScaleMultiplier = 1;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void Setup()
    {
        try
        {
            if (!bottlePrefab)
                bottlePrefab = Plugin.assetBundle.LoadAsset<GameObject>("Potion Bottle");
            var scale = (float)Math.Sqrt(GTPlayer.Instance.scale);

            NetworkPropertyHandler.Instance?.ChangeProperty(playerSizeKey, scale);
            sizeChanger = new GameObject("Grate Size Changer").AddComponent<SizeChanger>();
            sizeChangerTraverse = Traverse.Create(sizeChanger);
            minScale = sizeChangerTraverse.Field("minScale");
            maxScale = sizeChangerTraverse.Field("maxScale");
            sizeChangerTraverse.Field("myType").SetValue(SizeChanger.ChangerType.Static);
            sizeChangerTraverse.Field("staticEasing").SetValue(.5f);
            minScale.SetValue(scale);
            maxScale.SetValue(scale);

            holsterL = new GameObject("Holster (Left)").transform;
            shrinkPotion = Instantiate(bottlePrefab);
            SetupPotion(ref holsterL, ref shrinkPotion, true);

            holsterR = new GameObject("Holster (Right)").transform;
            growPotion = Instantiate(bottlePrefab);
            SetupPotion(ref holsterR, ref growPotion, false);
            ReloadConfiguration();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void SetupPotion(ref Transform holster, ref GameObject potion, bool isLeft)
    {
        try
        {
            holster.SetParent(GTPlayer.Instance.bodyCollider.transform, false);
            holster.localScale = Vector3.one;
            var offset = new Vector3(
                holsterOffset.x * (isLeft ? -1 : 1),
                holsterOffset.y,
                holsterOffset.z
            );
            holster.localPosition = offset;

            var sizePotion = potion.AddComponent<SizePotion>();
            sizePotion.name = isLeft ? "Grate Shrink Potion" : "Grate Grow Potion";
            sizePotion.Holster(holster);
            sizePotion.OnDrink += DrinkPotion;
            sizePotion.GetComponent<Renderer>().material = isLeft ? shrinkMaterial : growMaterial;
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void DrinkPotion(SizePotion potion)
    {
        var shrink = potion.gameObject == shrinkPotion;
        if (!shrink && !PositionValidator.Instance.isValidAndStable) return;
        var delta = shrink ? .99f : 1.01f;
        delta = PlayerExtensions.IsAdmin(PhotonNetwork.LocalPlayer)
            ? sizeChanger.MinScale * delta
            : Mathf.Clamp(sizeChanger.MinScale * delta, .03f, 20f);
        if (delta < 1)
            potion.gulp.pitch = MathExtensions.Map(GTPlayer.Instance.scale, 0, 1, 1.5f, 1);
        else
            potion.gulp.pitch = MathExtensions.Map(GTPlayer.Instance.scale, 1, 20, 1, .5f);
        minScale.SetValue(delta);
        maxScale.SetValue(delta);
        active = true;
    }

    protected override void Cleanup()
    {
        try
        {
            active = false;
            holsterL?.gameObject?.Obliterate();
            holsterR?.gameObject?.Obliterate();
            shrinkPotion?.gameObject?.Obliterate();
            growPotion?.gameObject?.Obliterate();
            sizeChanger?.gameObject.Obliterate();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return string.Format("- Grab the potion off of your waist with [Grip].\n" +
                             "- Pop the cork with the other [Grip].\n" +
                             "- Tilt the potion to drink it.\n\n" +
                             "Current size: {0:0.##}x", GTPlayer.Instance.scale);
    }

    protected override void ReloadConfiguration()
    {
        shrinkPotion.GetComponent<Renderer>().enabled = ShowPotions.Value;
        foreach (var rend in  shrinkPotion.GetComponentsInChildren<Renderer>())
        {
            rend.enabled = ShowPotions.Value;
        }
        growPotion.GetComponent<Renderer>().enabled = ShowPotions.Value;
        foreach (var rend in  growPotion.GetComponentsInChildren<Renderer>())
        {
            rend.enabled = ShowPotions.Value;
        }
    }

    public static void BindConfigEntries()
    {
        ShowNetworkedSizes = Plugin.configFile.Bind(
            DisplayName,
            "show networked size",
            true,
            "Whether or not to show how big other players using the Potions module are"
        );
        ShowPotions = Plugin.configFile.Bind(
            DisplayName,
            "show potions", 
            true,
            " Hide the local Bottles"
            );
    }

    public static void TryGetSizeChangerForRig(VRRig rig, out SizeChanger sc)
    {
        var size = rig.OwningNetPlayer.GetProperty<float>(playerSizeKey);
        if (!rig.HasProperty(playerSizeKey))
        {
            sc = null;
            return;
        }

        if (sizeChangers.ContainsKey(rig))
        {
            sc = sizeChangers[rig];
            var sizeChangerTraverse = Traverse.Create(sc);
            var minScale = sizeChangerTraverse.Field("minScale");
            var maxScale = sizeChangerTraverse.Field("maxScale");

            size = Mathf.Lerp(sc.MinScale, size, .75f * Time.fixedDeltaTime);
            minScale.SetValue(size);
            maxScale.SetValue(size);
        }
        else
        {
            size = Mathf.Lerp(rig.scaleFactor, size, .75f * Time.fixedDeltaTime);
            sc = CreateSizeChanger(size);
            sizeChangers.Add(rig, sc);
        }
    }

    public static SizeChanger CreateSizeChanger(float scale)
    {
        var sizeChanger = new GameObject("Grate Size Changer").AddComponent<SizeChanger>();
        var sizeChangerTraverse = Traverse.Create(sizeChanger);
        var minScale = sizeChangerTraverse.Field("minScale");
        var maxScale = sizeChangerTraverse.Field("maxScale");
        sizeChangerTraverse.Field("myType").SetValue(SizeChanger.ChangerType.Static);
        sizeChangerTraverse.Field("staticEasing").SetValue(.5f);
        minScale.SetValue(scale);
        maxScale.SetValue(scale);
        return sizeChanger;
    }
}

public class SizePotion : GrateGrabbable
{
    public Transform holster;
    public AudioSource gulp;
    private Cork cork;
    private Vector3 corkOffset, corkScale;
    private ParticleSystem drip;

    private bool isFlipped, wasFlipped, inRange;
    private Vector3 mouthPosition, bottlePosition;
    public Action<SizePotion> OnDrink;

    protected override void Awake()
    {
        try
        {
            base.Awake();
            gulp = GetComponent<AudioSource>();
            cork = transform.Find("Cork").gameObject.AddComponent<Cork>();
            cork.enabled = false;
            drip = transform.Find("Drip").GetComponent<ParticleSystem>();
            try
            {
                drip.gameObject.GetComponent<ParticleSystemRenderer>().material =
                    GetComponent<Renderer>().material;
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }

            corkOffset = cork.transform.localPosition;
            corkScale = cork.transform.localScale;
            LocalPosition = new Vector3(0.55f, 0, 0.425f);
            LocalRotation = new Vector3(8, 0, 0);
            throwOnDetach = false;
            OnSelectExit += (_, __) => { gulp.Stop(); };
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void FixedUpdate()
    {
        try
        {
            if (IsCorked())
            {
                wasFlipped = false;
                return;
            }

            isFlipped = Vector3.Dot(transform.up, Vector3.down) > 0;
            if (!wasFlipped && isFlipped)
                drip.Play();
            if (!isFlipped && wasFlipped)
                drip.Stop();
            wasFlipped = isFlipped;


            mouthPosition = GTPlayer.Instance.headCollider.transform.TransformPoint(new Vector3(0, -.05f, .1f));
            bottlePosition = transform.position;

            var range = .15f;
            var delta = bottlePosition - mouthPosition;
            inRange = Vector3.Dot(delta, Vector3.up) > 0f && delta.magnitude < range * GTPlayer.Instance.scale;
            if (isFlipped && inRange)
            {
                if (!gulp.isPlaying)
                    gulp.Play();
                OnDrink?.Invoke(this);
            }
            else
            {
                if (gulp.isPlaying)
                    gulp.Stop();
            }
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        cork?.gameObject.Obliterate();
    }

    private bool IsCorked()
    {
        return cork.transform.parent == transform;
    }

    public override void OnSelect(GrateInteractor interactor)
    {
        base.OnSelect(interactor);
        if (cork)
            cork.enabled = true;
    }

    public override void OnDeselect(GrateInteractor interactor)
    {
        base.OnDeselect(interactor);
        Holster(holster);
    }

    public override void OnPrimaryReleased(GrateInteractor interactor)
    {
        base.OnPrimaryReleased(interactor);
        if (IsCorked())
        {
            cork.Pop();
            cork.enabled = false;
        }
    }

    public override void OnActivate(GrateInteractor interactor)
    {
        base.OnActivate(interactor);
    }

    public void Holster(Transform holster)
    {
        drip.Stop();
        this.holster = holster;
        GetComponent<Rigidbody>().isKinematic = true;
        transform.SetParent(holster);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        cork.enabled = false;
        cork.rb.isKinematic = true;
        cork.transform.SetParent(transform);
        cork.transform.localPosition = corkOffset;
        cork.transform.localScale = corkScale;
        cork.transform.localRotation = Quaternion.identity;
        cork.shouldPlayPopSound = true;
    }
}

public class Cork : GrateGrabbable
{
    public Rigidbody rb;
    public bool shouldPlayPopSound = true;
    private AudioSource popSource;

    protected override void Awake()
    {
        base.Awake();
        LocalPosition = new Vector3(0.5f, .5f, 0.425f);
        LocalRotation = new Vector3(8, 0, 0);
        throwOnDetach = true;
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        popSource = GetComponent<AudioSource>();
    }

    public override void OnSelect(GrateInteractor interactor)
    {
        base.OnSelect(interactor);
        if (shouldPlayPopSound)
            popSource.Play();
        shouldPlayPopSound = false;
    }

    public void Pop()
    {
        transform.SetParent(null);
        rb.isKinematic = false;
        rb.velocity = transform.up * 2.5f;
        popSource.Play();
    }
}