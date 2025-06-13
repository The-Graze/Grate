using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using GorillaLocomotion;
using GorillaLocomotion.Swimming;
using GorillaNetworking;
using Grate.Extensions;
using Grate.Gestures;
using Grate.GUI;
using Grate.Modules;
using Grate.Networking;
using Grate.Tools;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace Grate
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static bool initialized, WaWa_graze_dot_cc;
        bool pluginEnabled = false;
        public static AssetBundle assetBundle;
        public static MenuController menuController;
        public static GameObject monkeMenuPrefab;
        public static ConfigFile configFile;
        internal static bool localPlayerTrusted;
        internal static bool localPlayerDev;

        public static bool IsSteam { get; protected set; }
        public static bool DebugMode { get; protected set; } = false;
        GestureTracker gt;
        NetworkPropertyHandler nph;
        public static GameObject Water;

        public void Setup()
        {
            gt = gameObject.GetOrAddComponent<GestureTracker>();
            nph = gameObject.GetOrAddComponent<NetworkPropertyHandler>();
            menuController = Instantiate(monkeMenuPrefab).AddComponent<MenuController>();
            localPlayerDev = PlayerExtensions.IsDev(PhotonNetwork.LocalPlayer);
            localPlayerTrusted = PlayerExtensions.IsTrusted(PhotonNetwork.LocalPlayer);
        }

        public void Cleanup()
        {
            try
            {
                Logging.Debug("Cleaning up");
                menuController?.gameObject?.Obliterate();
                gt?.Obliterate();
                nph?.Obliterate();
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }
        void Awake()
        {
            try
            {
                Instance = this;
                HarmonyPatches.ApplyHarmonyPatches();
                Logging.Init();
                configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "Grate.cfg"), true);
                Logging.Debug("Found", GrateModule.GetGrateModuleTypes().Count, "modules");
                foreach (Type moduleType in GrateModule.GetGrateModuleTypes())
                {
                    MethodInfo bindConfigs = moduleType.GetMethod("BindConfigEntries");
                    if (bindConfigs != null)
                    {
                        bindConfigs.Invoke(null, null);
                    }
                }
                MenuController.BindConfigEntries();
            }
            catch (Exception e) { Logging.Exception(e); }
        }

        void Start()
        {
            try
            {
                GorillaTagger.OnPlayerSpawned(OnGameInitialized);
                assetBundle = AssetUtils.LoadAssetBundle("Grate/Resources/gratebundle");
                monkeMenuPrefab = assetBundle.LoadAsset<GameObject>("Bark Menu");
                monkeMenuPrefab.name = "Grate Menu";
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        public static Text debugText;
        void CreateDebugGUI()
        {
            try
            {
                if (GTPlayer.Instance)
                {
                    var canvas = GTPlayer.Instance.headCollider.transform.GetComponentInChildren<Canvas>();
                    if (!canvas)
                    {
                        canvas = new GameObject("~~~Grate Debug Canvas").AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.WorldSpace;
                        canvas.transform.SetParent(GTPlayer.Instance.headCollider.transform);
                        canvas.transform.localPosition = Vector3.forward * .35f;
                        canvas.transform.localRotation = Quaternion.identity;
                        canvas.transform.localScale = Vector3.one;
                        canvas.gameObject.AddComponent<CanvasScaler>();
                        canvas.gameObject.AddComponent<GraphicRaycaster>();
                        canvas.GetComponent<RectTransform>().localScale = Vector3.one * .035f;
                        var text = new GameObject("~~~Text").AddComponent<Text>();
                        text.transform.SetParent(canvas.transform);
                        text.transform.localPosition = Vector3.zero;
                        text.transform.localRotation = Quaternion.identity;
                        text.transform.localScale = Vector3.one;
                        text.color = Color.green;
                        //text.text = "Hello World";
                        text.fontSize = 24;
                        text.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
                        text.alignment = TextAnchor.MiddleCenter;
                        text.horizontalOverflow = HorizontalWrapMode.Overflow;
                        text.verticalOverflow = VerticalWrapMode.Overflow;
                        text.color = Color.white;
                        text.GetComponent<RectTransform>().localScale = Vector3.one * .02f;
                        debugText = text;
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Exception(e);
            }
        }

        void OnGameInitialized()
        {
            Invoke("DelayedSetup", 2);
        }

        void DelayedSetup()
        {
            try
            {
                Logging.Debug("OnGameInitialized");
                initialized = true;
                PlatformTagJoin platform = (PlatformTagJoin)Traverse.Create(GorillaNetworking.PlayFabAuthenticator.instance).Field("platform").GetValue();
                Logging.Info("Platform: ", platform);
                IsSteam = platform.PlatformTag.Contains("Steam");

                NetworkSystem.Instance.OnJoinedRoomEvent += аaа;
                NetworkSystem.Instance.OnReturnedToSinglePlayer += аaа;
                Application.wantsToQuit += Quit;
                Water = Instantiate(FindObjectOfType<WaterVolume>().gameObject);
                Water.SetActive(false);
                MenuController.ShinyRocks = new Material[] 
                {
                    GameObject.Find("ShinyRock_Level4_Rocks").GetComponent<MeshRenderer>().materials[0],
                    GameObject.Find("ShinyRock_Level4_Rocks").GetComponent<MeshRenderer>().materials[0]
                };
                if (DebugMode)
                    CreateDebugGUI();
            }
            catch (Exception ex)
            {
                Logging.Exception(ex);
            }
        }

        private bool Quit()
        {
            if (NetworkSystem.Instance.InRoom)
            {
                NetworkSystem.Instance.OnReturnedToSinglePlayer += aQuit;
                NetworkSystem.Instance.ReturnToSinglePlayer();
                return false;
            }
            else
            {
                return true;
            }
        }

        private void aQuit()
        {
            WaWa_graze_dot_cc = false;
            Cleanup();
            Invoke("DelayQuit", 1);
        }

        void DelayQuit()
        {
            Application.Quit();
        }

        private void аaа()
        {
            StartCoroutine(Jоοin());
        }

        IEnumerator Jоοin()
        {
            Cleanup();
            yield return new WaitForSeconds(1);
            if (NetworkSystem.Instance.InRoom)
            {
                if (NetworkSystem.Instance.GameModeString.Contains("MODDED_"))
                {
                    WaWa_graze_dot_cc = true;
                    Setup();
                }
            }
            else
            {
                WaWa_graze_dot_cc = false;
                Cleanup();
            }
        }

        public void JoinLobby(string name)
        {
            StartCoroutine(JoinLobbyInternal(name));
        }

        IEnumerator JoinLobbyInternal(string name)
        {
            NetworkSystem.Instance.ReturnToSinglePlayer();
            yield return new WaitForSeconds(1.5f);
            PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(name, JoinType.Solo);
        }
    }
}