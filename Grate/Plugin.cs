using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
using Valve.Newtonsoft.Json;
using Console = Grate.Extensions.Console;

namespace Grate;

[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin? Instance;
    public static bool initialized, WaWa_graze_dot_cc;
    public static AssetBundle? assetBundle;
    public static MenuController? menuController;
    private static GameObject? monkeMenuPrefab;
    public static ConfigFile? configFile;
    public static bool localPlayerSupporter;
    public static bool localPlayerDev;
    public static bool localPlayerAdmin;
    public static GameObject? Water;

    public static Text? debugText;
    private GestureTracker? gt;
    private NetworkPropertyHandler? nph;

    public static bool IsSteam { get; private set; }
    public static bool DebugMode { get; protected set; } = false;

    private void Awake()
    {
        Instance = this;
        HarmonyPatches.ApplyHarmonyPatches();
        Logging.Init();
        configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "Grate.cfg"), true);
        foreach (var moduleType in GrateModule.GetGrateModuleTypes())
        {
            var bindConfigs = moduleType.GetMethod("BindConfigEntries");
            if (bindConfigs != null) bindConfigs.Invoke(null, null);
        }
        GorillaTagger.OnPlayerSpawned(OnGameInitialized);
        assetBundle = AssetUtils.LoadAssetBundle("Grate/Resources/gratebundle");
        monkeMenuPrefab = assetBundle?.LoadAsset<GameObject>("Bark Menu");
        monkeMenuPrefab!.name = "Grate Menu";
        MenuController.BindConfigEntries();

        Dictionary<string, string> tmp = new() { { "wawa", "wawa" } };
        var wawa = JsonConvert.SerializeObject(tmp);
        File.WriteAllText(Path.Combine(Paths.BepInExRootPath, "Ex.txt"),  wawa);
    }

    public void Setup()
    {
        gt = gameObject.GetOrAddComponent<GestureTracker>();
        nph = gameObject.GetOrAddComponent<NetworkPropertyHandler>();
        menuController = Instantiate(monkeMenuPrefab)?.AddComponent<MenuController>();
        localPlayerDev = NetworkSystem.Instance.LocalPlayer.IsDev();
        localPlayerAdmin =  NetworkSystem.Instance.LocalPlayer.IsAdmin();
        localPlayerSupporter = NetworkSystem.Instance.LocalPlayer.IsSupporter();
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
                    debugText = text;
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
            initialized = true;
            var platform = (PlatformTagJoin)Traverse.Create(PlayFabAuthenticator.instance).Field("platform").GetValue();
            Logging.Info("Platform: ", platform);
            IsSteam = platform.PlatformTag.Contains("Steam");

            NetworkSystem.Instance.OnJoinedRoomEvent += аaа;
            NetworkSystem.Instance.OnReturnedToSinglePlayer += аaа;
            Application.wantsToQuit += Quit;
            Water = Instantiate(FindObjectOfType<WaterVolume>().gameObject);
            Water.SetActive(false);
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
            NetworkSystem.Instance.OnReturnedToSinglePlayer += aQuit;
            NetworkSystem.Instance.ReturnToSinglePlayer();
            return false;
        }

        return true;
    }

    private void aQuit()
    {
        WaWa_graze_dot_cc = false;
        Cleanup();
        Invoke(nameof(DelayQuit), 1);
    }

    private void DelayQuit()
    {
        Application.Quit();
    }

    private void аaа()
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
                WaWa_graze_dot_cc = true;
                Setup();
            }
            else
            {
                WaWa_graze_dot_cc = false;
                Cleanup();
            }
        }
        else
        {
            WaWa_graze_dot_cc = false;
            Cleanup();
        }
    }

    public void JoinLobby(string LobbyName)
    {
        StartCoroutine(JoinLobbyInternal(LobbyName));
    }

    private IEnumerator JoinLobbyInternal(string LobbyName)
    {
        NetworkSystem.Instance.ReturnToSinglePlayer();
        yield return new WaitForSeconds(1.5f);
        PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(LobbyName, JoinType.Solo);
    }
}