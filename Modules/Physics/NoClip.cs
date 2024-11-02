using System;
using System.Collections;
using Grate.GUI;
using Grate.Patches;
using Grate.Tools;
using GorillaLocomotion;
using UnityEngine;
using Grate.Modules.Multiplayer;
using Grate.Modules.Movement;
using BepInEx.Configuration;
using Grate.Extensions;

namespace Grate.Modules.Physics
{
    public class NoClip : GrateModule
    {
        public static readonly string DisplayName = "No Clip";
        public static NoClip Instance;
        private LayerMask baseMask;
        private bool baseHeadIsTrigger, baseBodyIsTrigger;
        public static bool active;
        public static int layer = 29, layerMask = 1 << layer;
        GameObject acLocationMarker;
        bool flyWasEnabled;

        private struct GorillaTriggerInfo
        {
            public Collider collider;
            public bool wasEnabled;
        }

        void Awake() { Instance = this; }

        protected override void OnEnable()
        {
            try
            {
                if (!MenuController.Instance.Built) return;
                base.OnEnable();
                acLocationMarker = new GameObject("NoClipAcctivatePoint");
                acLocationMarker.transform.position = Player.Instance.bodyCollider.transform.position;
                acLocationMarker.transform.rotation = Player.Instance.turnParent.transform.rotation;
                if (!Piggyback.mounted)
                {
                    try
                    {
                        var fly = Plugin.menuController.GetComponent<Fly>();
                        flyWasEnabled = fly.enabled;
                        fly.enabled = true;
                    }
                    catch
                    {
                        Logging.Debug("Failed to enable fly for noclip.");
                    }
                }

                Logging.Debug("Disabling triggers");
                TriggerBoxPatches.triggersEnabled = false;
                baseMask = Player.Instance.locomotionEnabledLayers;
                Player.Instance.locomotionEnabledLayers = layerMask;

                baseBodyIsTrigger = Player.Instance.bodyCollider.isTrigger;
                Player.Instance.bodyCollider.isTrigger = true;

                baseHeadIsTrigger = Player.Instance.headCollider.isTrigger;
                Player.Instance.headCollider.isTrigger = true;
                active = true;
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        protected override void Cleanup() 
        {
            StartCoroutine(CleanupRoutine());
        }

        IEnumerator CleanupRoutine()
        {
            Logging.Debug("Cleaning up noclip");

            if (!active) yield break;
            Player.Instance.locomotionEnabledLayers = baseMask;
            Player.Instance.bodyCollider.isTrigger = baseBodyIsTrigger;
            Player.Instance.headCollider.isTrigger = baseHeadIsTrigger;
            Player.Instance.TeleportTo(acLocationMarker.transform, true);
            active = false;
            // Wait for the telport to complete
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            TriggerBoxPatches.triggersEnabled = true;
            Plugin.menuController.GetComponent<Fly>().enabled = flyWasEnabled;
            Logging.Debug("Enabling triggers");
            acLocationMarker?.Obliterate();
        }

        public override string GetDisplayName()
        {
            return DisplayName;
        }

        public override string Tutorial()
        {
            return "Effect: Disables collisions. Automatically enables Fly (Use the sticks to move).";
        }

    }
}
