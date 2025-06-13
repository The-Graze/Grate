using System;
using System.Collections.Generic;
using GorillaLocomotion;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Tools;
using UnityEngine;

namespace Grate.Modules.Multiplayer;

public class Telekinesis : GrateModule
{
    public static readonly string DisplayName = "Telekinesis";
    public static Telekinesis Instance;
    public SphereCollider tkCollider;
    private readonly List<TKMarker> markers = new();

    private Joint joint;
    private ParticleSystem playerParticles, sithlordHandParticles;
    private AudioSource sfx;
    private TKMarker sithLord;

    private void Awake()
    {
        Instance = this;
    }

    private void FixedUpdate()
    {
        if (Time.frameCount % 300 == 0)
            DistributeMidichlorians();

        if (!sithLord) TryGetSithLord();

        if (sithLord)
        {
            var rb = GTPlayer.Instance.bodyCollider.attachedRigidbody;
            if (!sithLord.IsGripping())
            {
                sithLord = null;
                sfx.Stop();
                sithlordHandParticles.Stop();
                sithlordHandParticles.Clear();
                playerParticles.Stop();
                playerParticles.Clear();
                rb.velocity = GTPlayer.Instance.bodyVelocityTracker.GetAverageVelocity(true) * 2;
                return;
            }

            var end = sithLord.controllingHand.position + sithLord.controllingHand.up * 3 * sithLord.rig.scaleFactor;
            var direction = end - GTPlayer.Instance.bodyCollider.transform.position;
            rb.AddForce(direction * 10, ForceMode.Impulse);
            var dampingThreshold = direction.magnitude * 10;
            //if (rb.velocity.magnitude > dampingThreshold)
            //if(direction.magnitude < 1)
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, .1f);
        }
    }

    protected override void OnEnable()
    {
        if (!MenuController.Instance.Built) return;
        base.OnEnable();
        try
        {
            ReloadConfiguration();
            var prefab = Plugin.assetBundle.LoadAsset<GameObject>("TK Hitbox");
            var hitbox = Instantiate(prefab);
            hitbox.name = "Grate TK Hitbox";
            hitbox.transform.SetParent(GTPlayer.Instance.bodyCollider.transform, false);
            hitbox.layer = GrateInteractor.InteractionLayer;
            tkCollider = hitbox.GetComponent<SphereCollider>();
            tkCollider.isTrigger = true;
            playerParticles = hitbox.GetComponent<ParticleSystem>();
            playerParticles.Stop();
            playerParticles.Clear();
            sfx = hitbox.GetComponent<AudioSource>();

            var sithlordEffect = Instantiate(prefab);
            sithlordEffect.name = "Grate Sithlord Particles";
            sithlordEffect.transform.SetParent(GTPlayer.Instance.bodyCollider.transform, false);
            sithlordEffect.layer = GrateInteractor.InteractionLayer;
            sithlordHandParticles = sithlordEffect.GetComponent<ParticleSystem>();
            var shape = sithlordHandParticles.shape;
            shape.radius = .2f;
            shape.position = Vector3.zero;
            Destroy(sithlordEffect.GetComponent<SphereCollider>());
            DistributeMidichlorians();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void TryGetSithLord()
    {
        foreach (var tk in markers)
            try
            {
                if (tk && tk.IsGripping() && tk.PointingAtMe())
                {
                    sithLord = tk;
                    playerParticles.Play();
                    sithlordHandParticles.transform.SetParent(tk.controllingHand);
                    sithlordHandParticles.transform.localPosition = Vector3.zero;
                    sithlordHandParticles.Play();
                    sfx.Play();
                    break;
                }
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
    }

    private void DistributeMidichlorians()
    {
        foreach (var rig in GorillaParent.instance.vrrigs)
            try
            {
                if (rig.OwningNetPlayer.IsLocal ||
                    rig.gameObject.GetComponent<TKMarker>()) continue;

                markers.Add(rig.gameObject.AddComponent<TKMarker>());
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
    }

    protected override void Cleanup()
    {
        foreach (var m in markers) m?.Obliterate();
        tkCollider?.gameObject?.Obliterate();
        sithlordHandParticles?.gameObject?.Obliterate();
        joint?.Obliterate();
        sithLord = null;
        markers.Clear();
        tkCollider = null;
    }

    public override string GetDisplayName()
    {
        return DisplayName;
    }

    public override string Tutorial()
    {
        return "Effect: If another player points their index finger at you, they can pick you up with telekinesis.";
    }

    public class TKMarker : MonoBehaviour
    {
        public static int count;
        public VRRig rig;
        public Transform leftHand, rightHand, controllingHand;
        public Rigidbody controllingBody;
        private DebugRay dr;
        private bool grippingRight, grippingLeft;
        private int uuid;

        private void Awake()
        {
            rig = GetComponent<VRRig>();
            uuid = count++;
            leftHand = SetupHand("L");
            rightHand = SetupHand("R");
            dr = new GameObject($"{uuid} (Debug Ray)").AddComponent<DebugRay>();
        }

        private void OnDestroy()
        {
            dr?.gameObject?.Obliterate();
            leftHand?.GetComponent<Rigidbody>()?.Obliterate();
            rightHand?.GetComponent<Rigidbody>()?.Obliterate();
        }

        public Transform SetupHand(string hand)
        {
            var handTransform = transform.Find(
                string.Format(GestureTracker.palmPath, hand).Substring(1)
            );
            var rb = handTransform.gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            return handTransform;
        }

        public bool IsGripping()
        {
            grippingRight =
                rig.rightIndex.calcT < .5f &&
                rig.rightMiddle.calcT > .5f;
            //rig.rightThumb.calcT > .5f;

            grippingLeft =
                rig.leftIndex.calcT < .5f &&
                rig.leftMiddle.calcT > .5f;
            //rig.leftThumb.calcT > .5f;
            return grippingRight || grippingLeft;
        }

        public bool PointingAtMe()
        {
            try
            {
                if (!(grippingRight || grippingLeft)) return false;
                var hand = grippingRight ? rightHand : leftHand;
                controllingHand = hand;
                if (!hand) return false;
                controllingBody = hand?.GetComponent<Rigidbody>();
                if (!controllingBody) return false;
                RaycastHit hit;
                var ray = new Ray(hand.position, hand.up);
                Logging.Debug("DOING THE THING WITH THE COLLIDER");
                var collider = Instance.tkCollider;
                UnityEngine.Physics.SphereCast(ray, .2f * GTPlayer.Instance.scale, out hit, collider.gameObject.layer);
                return hit.collider == collider;
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }

            return false;
        }
    }
}