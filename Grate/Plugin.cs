using System;
using System.Collections;
using System.IO;
using System.Linq;
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
using UnityEngine;
using UnityEngine.UI;
using Console = Grate.Extensions.Console;

namespace Grate;

[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin? Instance;
    public static bool Initialized, WaWaGrazeDotCc;
    public static AssetBundle? AssetBundle;
    public static MenuController? MenuController;
    private static GameObject? monkeMenuPrefab;
    public static ConfigFile? ConfigFile;
    public static bool LocalPlayerSupporter;
    public static bool LocalPlayerDev;
    public static bool LocalPlayerAdmin;
    public static GameObject? Water;

    public static Text? DebugText;
    private GestureTracker? gt;
    private NetworkPropertyHandler? nph;

    public static bool IsSteam { get; private set; }
    public static bool DebugMode { get; protected set; } = false;

    private void Awake()
    {
        Instance = this;
        HarmonyPatches.ApplyHarmonyPatches();
        Logging.Init();
        ConfigFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "Grate.cfg"), true);
        var list = GrateModule.GetGrateModuleTypes();
        foreach (var bindConfigs in list.Select(moduleType => moduleType.GetMethod("BindConfigEntries")).Select(info => info).OfType<MethodInfo>())
        {
            bindConfigs.Invoke(null, null);
        }

        GorillaTagger.OnPlayerSpawned(OnGameInitialized);
        AssetBundle = AssetUtils.LoadAssetBundle("Grate/Resources/gratebundle");
        monkeMenuPrefab = AssetBundle?.LoadAsset<GameObject>("Bark Menu");
        monkeMenuPrefab!.name = "Grate Menu";
        MenuController.BindConfigEntries();
    }

    public void Setup()
    {
        gt = gameObject.GetOrAddComponent<GestureTracker>();
        nph = gameObject.GetOrAddComponent<NetworkPropertyHandler>();
        MenuController = Instantiate(monkeMenuPrefab)?.AddComponent<MenuController>();
        LocalPlayerDev = NetworkSystem.Instance.LocalPlayer.IsDev();
        LocalPlayerAdmin = NetworkSystem.Instance.LocalPlayer.IsAdmin();
        LocalPlayerSupporter = NetworkSystem.Instance.LocalPlayer.IsSupporter();
    }

    public void Cleanup()
    {
        try
        {
            MenuController?.gameObject.Obliterate();
            gt?.Obliterate();
            nph?.Obliterate();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void CreateDebugGUI()
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
                    DebugText = text;
                }
            }
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void OnGameInitialized()
    {
        Invoke(nameof(DelayedSetup), 2);
    }

    private void DelayedSetup()
    {
        try
        {
            Logging.Debug("OnGameInitialized");
            Initialized = true;
            var platform = (PlatformTagJoin)Traverse.Create(PlayFabAuthenticator.instance).Field("platform").GetValue();
            Logging.Info("Platform: ", platform);
            IsSteam = platform.PlatformTag.Contains("Steam");

            NetworkSystem.Instance.OnJoinedRoomEvent += Аaа;
            NetworkSystem.Instance.OnReturnedToSinglePlayer += Аaа;
            Application.wantsToQuit += Quit;
            Water = Instantiate(FindObjectOfType<WaterVolume>().gameObject);
            Water.SetActive(false);
            gameObject.AddComponent<CoroutineManager>();
            gameObject.AddComponent<ServerData>();
            gameObject.AddComponent<Console>();
            MenuController.ShinyRocks =
            [
                GameObject.Find("ShinyRock_Level4_Rocks").GetComponent<MeshRenderer>().materials[0],
                GameObject.Find("ShinyRock_Level4_Rocks").GetComponent<MeshRenderer>().materials[0]
            ];
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
            NetworkSystem.Instance.OnReturnedToSinglePlayer += AQuit;
            NetworkSystem.Instance.ReturnToSinglePlayer();
            return false;
        }

        return true;
    }

    private void AQuit()
    {
        WaWaGrazeDotCc = false;
        Cleanup();
        Invoke(nameof(DelayQuit), 1);
    }

    private void DelayQuit()
    {
        Application.Quit();
    }

    private void Аaа()
    {
        StartCoroutine(Jоοin());
    }

    private IEnumerator Jоοin()
    {
        Cleanup();
        yield return new WaitForSeconds(1);
        if (NetworkSystem.Instance.InRoom)
        {
            if (NetworkSystem.Instance.GameModeString.Contains("MODDED_"))
            {
                WaWaGrazeDotCc = true;
                Setup();
            }
            else
            {
                WaWaGrazeDotCc = false;
                Cleanup();
            }
        }
        else
        {
            WaWaGrazeDotCc = false;
            Cleanup();
        }
    }

    public void JoinLobby(string lobbyName)
    {
        StartCoroutine(JoinLobbyInternal(lobbyName));
    }

    private IEnumerator JoinLobbyInternal(string lobbyName)
    {
        if (NetworkSystem.Instance.InRoom)
            if (NetworkSystem.Instance.RoomName == lobbyName)
                NetworkSystem.Instance.ReturnToSinglePlayer();

        yield return new WaitForSeconds(3);
        PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(lobbyName, JoinType.Solo);
    }
}