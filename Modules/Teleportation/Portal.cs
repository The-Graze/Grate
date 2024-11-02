using GorillaLocomotion;
using Grate.Tools;
using System;
using UnityEngine;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using BepInEx.Configuration;
using UnityEngine.XR;
using System.Collections.Generic;

namespace Grate.Modules.Teleportation
{
    public class Portal : GrateModule
    {
        public static readonly string DisplayName = "Portals";
        public static GameObject launcherPrefab, bluePortal, orangePortal;
        public GameObject launcher;
        ParticleSystem[] smokeSystems;
        AudioSource orangeAudio;
        AudioSource blueAudio;
        XRNode hand;
        Dictionary<int, GameObject> portals = new Dictionary<int, GameObject>();

        void Awake()
        {
        }


        protected override void OnEnable()
        {

            if (!MenuController.Instance.Built) return;
            base.OnEnable();
            try
            {
                if (!launcherPrefab)
                {
                    launcherPrefab = Plugin.portalAssetBundle.LoadAsset<GameObject>("PortalGun");
                    orangePortal = Plugin.portalAssetBundle.LoadAsset<GameObject>("OrangePortal");
                    bluePortal = Plugin.portalAssetBundle.LoadAsset<GameObject>("BluePortal");
                }

                launcher = Instantiate(launcherPrefab);
                orangeAudio = launcher.transform.Find("OrangeAudio").GetComponent<AudioSource>();
                blueAudio = launcher.transform.Find("BlueAudio").GetComponent<AudioSource>();
                launcher.transform.Find("PortalBeam").Obliterate();

                ReloadConfiguration();

                smokeSystems = launcher.GetComponentsInChildren<ParticleSystem>();
                foreach (var system in smokeSystems)
                    system.gameObject.SetActive(false);

                HideLauncher(null);

            }
            catch (Exception e) { Logging.Exception(e); }
        }

        void ShowLauncher(InputTracker _)
        {
            launcher.SetActive(true);
            foreach (var system in smokeSystems)
                system.gameObject.SetActive(false);
        }

        void HideLauncher(InputTracker _)
        {
            launcher.SetActive(false);
            foreach (var system in smokeSystems)
                system.gameObject.SetActive(false);
        }

        void Fire(int portal)
        {
            if (!launcher.activeSelf) return;
            MakePortal(portal);
        }

        void MakePortal(int portal)
        {
            RaycastHit hit = Raycast(launcher.transform.GetChild(0).position, launcher.transform.GetChild(0).transform.forward);
            if (!hit.collider) return;
            MakePortal(hit.point, hit.normal, portal);

            // try
            // {
            // 
            // 
            // }
            // catch (Exception e) { Logging.Exception(e); }
        }

        GameObject MakePortal(Vector3 position, Vector3 normal, int index)
        {
            GameObject portal = null;
            try
            {
                // yes i know this is a dogshit way to do it but im tired and cant be fucked finding another way
                portals[index]?.Obliterate();
                portals.Remove(index);
            }
            catch (Exception e) { }
            if (index == 0)
            {
                portal = Instantiate(orangePortal);
                orangeAudio.PlayOneShot(orangeAudio.clip, 1f);
                smokeSystems[0].startColor = new Color(255, 160, 0);
            }
            else
            {
                portal = Instantiate(bluePortal);
                blueAudio.PlayOneShot(blueAudio.clip, 1f);
                smokeSystems[0].startColor = new Color(0, 160, 255);
            }
            GestureTracker.Instance.HapticPulse(false, 1, .25f);
            foreach (var system in smokeSystems)
            {
                system.gameObject.SetActive(true);
                system.Clear();
                system.Play();
            }
            portal.transform.position = position;
            Logging.Info("Creating portal with index: " + index);
            portal.transform.LookAt(position + normal);
            portal.transform.position += portal.transform.forward / 100;
            portal.transform.localScale = portal.transform.localScale * GetPortalSize(PortalSize.Value);
            portals.Add(index, portal);
            portal.AddComponent<CollisionObserver>().OnTriggerEntered += (self, collider) =>
            {
                if (collider.gameObject.GetComponentInParent<Player>() ||
                    collider == GestureTracker.Instance.leftPalmInteractor ||
                    collider == GestureTracker.Instance.rightPalmInteractor)
                    OnPlayerEntered(self, index);
            };
            return portal;
        }

        float GetPortalSize(string value)
        {
            switch (value)
            {
                default:
                    return 1;
                case "small":
                    return 0.5f;
                case "normal":
                    return 1;
                case "big":
                    return 1.5f;
            }
        }

        void OnPlayerEntered(GameObject inPortal, int portalIndex)
        {
            GameObject outPortal = null;
            if (portalIndex == 1)
            {
                outPortal = portals[0];
            }
            else
            {
                outPortal = portals[1];
            }
            if (!outPortal) return;
            float p = Player.Instance.RigidbodyVelocity.magnitude;
            Player.Instance.TeleportTo(outPortal.transform, true);
            Player.Instance.SetVelocity(p * outPortal.transform.forward);
        }

        RaycastHit Raycast(Vector3 origin, Vector3 forward)
        {
            Ray ray = new Ray(origin, forward);
            RaycastHit hit;

            // Shoot a ray forward
            UnityEngine.Physics.Raycast(ray, out hit, Mathf.Infinity, Teleport.layerMask);
            return hit;
        }

        protected override void Cleanup()
        {
            if (!MenuController.Instance.Built) return;
            UnsubscribeFromEvents();
            launcher?.Obliterate();
            foreach (GameObject portal in portals.Values)
            {
                portal?.Obliterate();
            }
            portals.Clear();
        }

        public static ConfigEntry<string> LauncherHand;
        public static ConfigEntry<string> PortalSize;
        protected override void ReloadConfiguration()
        {
            UnsubscribeFromEvents();

            hand = LauncherHand.Value == "left"
                ? XRNode.LeftHand : XRNode.RightHand;

            Parent();

            InputTracker grip = GestureTracker.Instance.GetInputTracker("grip", hand);
            InputTracker primary = GestureTracker.Instance.GetInputTracker("primary", hand);
            InputTracker secondary = GestureTracker.Instance.GetInputTracker("secondary", hand);

            grip.OnPressed += ShowLauncher;
            grip.OnReleased += HideLauncher;
            primary.OnPressed += FireA;
            secondary.OnPressed += FireB;
            foreach (GameObject portal in portals.Values)
            {
                portal.transform.localScale = new Vector3(0.01384843f, 0.01717813f, 0.01384843f) * GetPortalSize(PortalSize.Value);
            }
        }

        void FireA(InputTracker _) { Fire(0); }
        void FireB(InputTracker _) { Fire(1); }

        void Parent()
        {
            Transform parent = GestureTracker.Instance.rightHand.transform;
            Vector3 position = new Vector3(0.637f, -0.1155f, 3.8735f);
            Vector3 rotation = new Vector3(89.7736f, 302.1569f, 208.3616f);
            if (hand == XRNode.LeftHand)
            {
                parent = GestureTracker.Instance.leftHand.transform;
                position = new Vector3(0.637f, -0.1155f, 3.8735f);
                rotation = new Vector3(89.7736f, 302.1569f, 208.3616f);
            }
            //-0.00002

            launcher.transform.SetParent(parent, true);
            launcher.transform.localPosition = position;
            launcher.transform.localRotation = Quaternion.Euler(rotation);
        }

        void UnsubscribeFromEvents()
        {
            InputTracker grip = GestureTracker.Instance.GetInputTracker("grip", hand);
            InputTracker primary = GestureTracker.Instance.GetInputTracker("primary", hand);
            InputTracker secondary = GestureTracker.Instance.GetInputTracker("secondary", hand);
            grip.OnPressed -= ShowLauncher;
            grip.OnReleased -= HideLauncher;
            primary.OnPressed -= FireA;
            secondary.OnPressed -= FireB;
        }

        public static void BindConfigEntries()
        {
            LauncherHand = Plugin.configFile.Bind(
                section: DisplayName,
                key: "launcher hand",
                defaultValue: "right",
                configDescription: new ConfigDescription(
                    "Which hand holds the launcher",
                    new AcceptableValueList<string>("left", "right")
                )
            );
            PortalSize = Plugin.configFile.Bind(
                section: DisplayName,
                key: "Portal Size",
                defaultValue: "normal",
                configDescription: new ConfigDescription(
                    "The size of the portals",
                    new AcceptableValueList<string>("small", "normal", "big")
                )
            );
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            string h = LauncherHand.Value.Substring(0, 1).ToUpper() + LauncherHand.Value.Substring(1);
            return $"Hold [{h} Grip] to summon the portal cannon. Use [{h} A / B] to fire the portals.";
        }
    }
}
