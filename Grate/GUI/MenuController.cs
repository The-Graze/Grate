using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Grate.Extensions;
using Grate.Gestures;
using Grate.Interaction;
using Grate.Modules;
using Grate.Modules.Misc;
using Grate.Modules.Movement;
using Grate.Modules.Multiplayer;
using Grate.Modules.Physics;
using Grate.Modules.Teleportation;
using Grate.Tools;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR;
using Player = GorillaLocomotion.GTPlayer;

namespace Grate.GUI;

public class MenuController : GrateGrabbable
{
    public static MenuController Instance;
    public static InputTracker SummonTracker;
    public static ConfigEntry<string> SummonInput;
    public static ConfigEntry<string> SummonInputHand;
    public static ConfigEntry<string> Theme;
    public static ConfigEntry<bool> Festive;
    public static Material[] ShinyRocks;

    public static bool debugger = true;

    public Vector3
        initialMenuOffset = new(0, .035f, .65f),
        btnDimensions = new(.3f, .05f, .05f);

    public Rigidbody _rigidbody;
    public List<Transform> modPages;
    public List<ButtonController> buttons;
    public List<GrateModule> modules = new();
    public GameObject modPage, settingsPage;
    public Text helpText;

    public Renderer? renderer;
    public Material[]? grate, bark, hollopurp, monke, old;

    private int debugButtons;

    private bool docked;

    private int pageIndex;
    public bool Built { get; private set; }

    protected override void Awake()
    {
        if (!NetworkSystem.Instance.GameModeString.Contains("MODDED_")) return;
        Instance = this;
        try
        {
            base.Awake();
            throwOnDetach = true;
            gameObject.AddComponent<PositionValidator>();
            if (Plugin.configFile != null) Plugin.configFile.SettingChanged += SettingsChanged;
            var tooAddmodules = new List<GrateModule>
            {
                // Locomotion
                gameObject.AddComponent<Airplane>(),
                gameObject.AddComponent<Helicopter>(),
                gameObject.AddComponent<Bubble>(),
                gameObject.AddComponent<Fly>(),
                gameObject.AddComponent<HandFly>(),
                gameObject.AddComponent<GrapplingHooks>(),
                gameObject.AddComponent<Climb>(),
                gameObject.AddComponent<DoubleJump>(),
                gameObject.AddComponent<Platforms>(),
                gameObject.AddComponent<Frozone>(),
                gameObject.AddComponent<NailGun>(),
                gameObject.AddComponent<Rockets>(),
                gameObject.AddComponent<SpeedBoost>(),
                gameObject.AddComponent<Swim>(),
                gameObject.AddComponent<Wallrun>(),
                gameObject.AddComponent<Zipline>(),

                //// Physics
                gameObject.AddComponent<LowGravity>(),
                gameObject.AddComponent<NoClip>(),
                gameObject.AddComponent<NoSlip>(),
                gameObject.AddComponent<Potions>(),
                gameObject.AddComponent<SlipperyHands>(),
                gameObject.AddComponent<DisableWind>(),

                //// Teleportation
                gameObject.AddComponent<Checkpoint>(),
                gameObject.AddComponent<Portal>(),
                gameObject.AddComponent<Pearl>(),
                gameObject.AddComponent<Teleport>(),

                //// Multiplayer
                gameObject.AddComponent<Boxing>(),
                gameObject.AddComponent<Piggyback>(),
                gameObject.AddComponent<Telekinesis>(),
                gameObject.AddComponent<Grab>(),
                gameObject.AddComponent<Fireflies>(),
                gameObject.AddComponent<ESP>(),
                gameObject.AddComponent<RatSword>(),
                gameObject.AddComponent<Kamehameha>()

                //// Misc
                //gameObject.AddComponent<ReturnToVS>(),
                //gameObject.AddComponent<Lobby>(),
            };
            var meow = gameObject.AddComponent<CatMeow>();
            if (NetworkSystem.Instance.LocalPlayer.UserId == "FBE3EE50747CB892") modules.Add(meow);
            var sb = gameObject.AddComponent<StoneBroke>();
            if (NetworkSystem.Instance.LocalPlayer.UserId == "CA8FDFF42B7A1836") modules.Add(sb);
            var bs = gameObject.AddComponent<BagHammer>();
            if (NetworkSystem.Instance.LocalPlayer.UserId == "9ABD0C174289F58E") modules.Add(bs);
            var g = gameObject.AddComponent<Grazing>();
            if (NetworkSystem.Instance.LocalPlayer.UserId == "42D7D32651E93866") modules.Add(g);
            var ch = gameObject.AddComponent<Cheese>();
            if (NetworkSystem.Instance.LocalPlayer.UserId == "B1B20DEEEDB71C63") modules.Add(ch);
            var goudabudaHat = gameObject.AddComponent<GoudabudaHat>();
            if (NetworkSystem.Instance.LocalPlayer.UserId == "A48744B93D9A3596") modules.Add(goudabudaHat);
            var supporterMod = gameObject.AddComponent<Supporter>();
            if (NetworkSystem.Instance.LocalPlayer.IsSupporter()) modules.Add(supporterMod);
            var developerMod = gameObject.AddComponent<Developer>();
            if (NetworkSystem.Instance.LocalPlayer.IsDev()) modules.Add(developerMod);
            modules.AddRange(tooAddmodules);
            ReloadConfiguration();
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    private void Start() // sigma sigma sigma s
    {
        Summon();
        transform.SetParent(null);
        transform.position = Vector3.zero;
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        transform.SetParent(null);
        AddBlockerToAllButtons(ButtonController.Blocker.MENU_FALLING);
        docked = false;
    }

    private void FixedUpdate()
    {
        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (!docked)
            {
                Summon();
            }
            else
            {
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
                transform.SetParent(null);
                AddBlockerToAllButtons(ButtonController.Blocker.MENU_FALLING);
                docked = false;
            }
        }

        if (PhotonNetwork.InRoom && !NetworkSystem.Instance.GameModeString.Contains("MODDED")) gameObject.Obliterate();

        // The potions tutorial needs to be updated frequently to keep the current size
        // up-to-date, even when the mod is disabled
        if (GrateModule.LastEnabled && GrateModule.LastEnabled == Potions.Instance)
            helpText.text = Potions.Instance.Tutorial();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Plugin.configFile!.SettingChanged -= SettingsChanged;
    }

private void ThemeChanged()
{
    if (Plugin.assetBundle != null)
    {
        if (!renderer)
            renderer = GetComponent<MeshRenderer>();
        if (Plugin.assetBundle != null)
        {
            if (grate == null)
            {
                var zipline = Plugin.assetBundle.LoadAsset<Material>("Zipline Rope Material");
                var metal = Plugin.assetBundle.LoadAsset<Material>("Metal Material");
                if (zipline && metal)
                {
                    grate = [zipline, metal];
                }
            }

            if (bark == null)
            {
                var outer = Plugin.assetBundle.LoadAsset<Material>("m_Menu Outer");
                var inner = Plugin.assetBundle.LoadAsset<Material>("m_Menu Inner");
                if (outer && inner)
                {
                    bark = [outer, inner];
                }
            }

            if (hollopurp == null)
            {
                var sparkleMat = Plugin.assetBundle.LoadAsset<Material>("m_TK Sparkles");
                if (sparkleMat)
                {
                    hollopurp = [sparkleMat, sparkleMat];
                }
            }

            if (monke == null || old == null)
            {
                var baseMat = Plugin.assetBundle.LoadAsset<Material>("Gorilla Material");
                if (baseMat)
                {
                    monke ??= [baseMat, baseMat];

                    old ??=
                    [
                        new Material(baseMat)
                        {
                            mainTexture = null,
                            color = new Color(0.17f, 0.17f, 0.17f)
                        },
                        new Material(baseMat)
                        {
                            mainTexture = null,
                            color = new Color(0.2f, 0.2f, 0.2f)
                        }
                    ];
                }
            }
        }

        var themeName = Theme.Value.ToLower();

        switch (themeName)
        {
            case "grate":
                if (grate != null) renderer.materials = grate;
                break;

            case "bark":
                renderer.materials = bark;
                break;

            case "holowpurple":
                renderer.materials = hollopurp;
                break;

            case "oldgrate":
                renderer.materials = old;
                break;

            case "shinyrocks":
                renderer.materials = ShinyRocks;
                break;

            case "player" when VRRig.LocalRig.CurrentCosmeticSkin != null:
                var skinMat = VRRig.LocalRig.CurrentCosmeticSkin.scoreboardMaterial;
                renderer.materials = [skinMat, skinMat];
                break;

            case "player":
                if (monke != null)
                {
                    renderer.materials = monke;
                    var playerColor = VRRig.LocalRig.playerColor;
                    monke[0].color = playerColor;
                    monke[1].color = playerColor;
                }

                break;
        }
    }
    transform.GetChild(5).gameObject.SetActive(Festive.Value);
}


    private void ReloadConfiguration()
    {
        if (SummonTracker != null)
            SummonTracker.OnPressed -= Summon;
        GestureTracker.Instance.OnMeatBeat -= Summon;

        var hand = SummonInputHand.Value == "left"
            ? XRNode.LeftHand
            : XRNode.RightHand;

        if (SummonInput.Value == "gesture")
        {
            GestureTracker.Instance.OnMeatBeat += Summon;
        }
        else
        {
            SummonTracker = GestureTracker.Instance.GetInputTracker(
                SummonInput.Value, hand
            );
            if (SummonTracker != null)
                SummonTracker.OnPressed += Summon;
        }
    }

    private void SettingsChanged(object sender, SettingChangedEventArgs e)
    {
        if (e.ChangedSetting == SummonInput || e.ChangedSetting == SummonInputHand) ReloadConfiguration();
        if (e.ChangedSetting == Theme || e.ChangedSetting == Festive) ThemeChanged();
    }

    private void Summon(InputTracker _)
    {
        Summon();
    }

    public void Summon()
    {
        if (!Built)
            BuildMenu();
        else
            ResetPosition();
    }

    private void ResetPosition()
    {
        _rigidbody.isKinematic = true;
        _rigidbody.velocity = Vector3.zero;
        transform.SetParent(Player.Instance.bodyCollider.transform);
        transform.localPosition = initialMenuOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        foreach (var button in buttons) button.RemoveBlocker(ButtonController.Blocker.MENU_FALLING);
        docked = true;
    }

    private IEnumerator VerCheck()
    {
        using (var request = UnityWebRequest.Get("https://raw.githubusercontent.com/The-Graze/Grate/master/ver.txt"))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                var fileContents = request.downloadHandler.text;

                var checkedV = new Version(fileContents);
                var localv = new Version(PluginInfo.Version);

                if (checkedV > localv)
                {
                    gameObject.transform.Find("Version Canvas").GetComponentInChildren<Text>().horizontalOverflow =
                        HorizontalWrapMode.Overflow;
                    gameObject.transform.Find("Version Canvas").GetComponentInChildren<Text>().verticalOverflow =
                        VerticalWrapMode.Overflow;
                    gameObject.transform.Find("Version Canvas").GetComponentInChildren<Text>().text =
                        "!!Update Needed!! \n GoTo: \n https://graze.cc/grate";
                }
                else
                {
                    gameObject.transform.Find("Version Canvas").GetComponentInChildren<Text>().text =
                        $"{PluginInfo.Name} {PluginInfo.Version}";
                }
            }
        }
    }

    private void BuildMenu()
    {
        Logging.Debug("Building menu...");
        try
        {
            helpText = gameObject.transform.Find("Help Canvas").GetComponentInChildren<Text>();
            helpText.text = "Enable a module to see its tutorial.";
            StartCoroutine(VerCheck());
            var collider = gameObject.GetOrAddComponent<BoxCollider>();
            collider.isTrigger = true;
            _rigidbody = gameObject.GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;

            SetupInteraction();
            SetupModPages();
            SetupSettingsPage();

            transform.SetParent(Player.Instance.bodyCollider.transform);
            ResetPosition();
            Logging.Debug("Build successful.");
            ReloadConfiguration();
            ThemeChanged();
        }
        catch (Exception ex)
        {
            Logging.Warning(ex.Message);
            Logging.Warning(ex.StackTrace);
            return;
        }

        Built = true;
    }

    private void SetupSettingsPage()
    {
        var button = gameObject.transform.Find("Settings Button").gameObject;
        var btnController = button.AddComponent<ButtonController>();
        buttons.Add(btnController);
        btnController.OnPressed += (obj, pressed) =>
        {
            settingsPage.SetActive(pressed);
            if (pressed)
                settingsPage.GetComponent<SettingsPage>().UpdateText();
            modPage.SetActive(!pressed);
        };

        settingsPage = transform.Find("Settings Page").gameObject;
        settingsPage.AddComponent<SettingsPage>();
        settingsPage.SetActive(false);
    }

    public void SetupModPages()
    {
        var modPageTemplate = gameObject.transform.Find("Mod Page");
        var buttonsPerPage = modPageTemplate.childCount - 2; // Excludes the prev/next page btns
        var numPages = (modules.Count - 1) / buttonsPerPage + 1;
        if (Plugin.DebugMode)
            numPages++;

        modPages = new List<Transform> { modPageTemplate };
        for (var i = 0; i < numPages - 1; i++)
            modPages.Add(Instantiate(modPageTemplate, gameObject.transform));

        buttons = new List<ButtonController>();
        for (var i = 0; i < modules.Count; i++)
        {
            var module = modules[i];

            var page = modPages[i / buttonsPerPage];
            var button = page.Find($"Button {i % buttonsPerPage}").gameObject;

            var btnController = button.AddComponent<ButtonController>();
            buttons.Add(btnController);
            btnController.OnPressed += (obj, pressed) =>
            {
                module.enabled = pressed;
                if (pressed)
                    helpText.text = module.GetDisplayName().ToUpper() +
                                    "\n\n" + module.Tutorial().ToUpper();
            };
            module.button = btnController;
            btnController.SetText(module.GetDisplayName().ToUpper());
        }

        AddDebugButtons();

        foreach (var modPage in modPages)
        {
            foreach (Transform button in modPage)
                if (button.name == "Button Left" && modPage != modPages[0])
                {
                    var btnController = button.gameObject.AddComponent<ButtonController>();
                    btnController.OnPressed += PreviousPage;
                    btnController.SetText("Prev Page");
                    buttons.Add(btnController);
                }
                else if (button.name == "Button Right" && modPage != modPages[modPages.Count - 1])
                {
                    var btnController = button.gameObject.AddComponent<ButtonController>();
                    btnController.OnPressed += NextPage;
                    btnController.SetText("Next Page");
                    buttons.Add(btnController);
                }
                else if (!button.GetComponent<ButtonController>())
                {
                    button.gameObject.SetActive(false);
                }

            modPage.gameObject.SetActive(false);
        }

        modPageTemplate.gameObject.SetActive(true);
        modPage = modPageTemplate.gameObject;
    }

    private void AddDebugButtons()
    {
        AddDebugButton("Debug Log", (btn, isPressed) =>
        {
            debugger = isPressed;
            Logging.Debug("Debugger", debugger ? "active" : "inactive");
            Plugin.debugText.text = "";
        });

        AddDebugButton("Close game", (btn, isPressed) =>
        {
            debugger = isPressed;
            if (btn.text.text == "You sure?")
                Application.Quit();
            else
                btn.text.text = "You sure?";
        });

        AddDebugButton("Show Colliders", (btn, isPressed) =>
        {
            if (isPressed)
                foreach (var c in FindObjectsOfType<Collider>())
                    c.gameObject.AddComponent<ColliderRenderer>();
            else
                foreach (var c in FindObjectsOfType<ColliderRenderer>())
                    c.Obliterate();
        });
    }

    private void AddDebugButton(string title, Action<ButtonController, bool> onPress)
    {
        if (!Plugin.DebugMode) return;
        var page = modPages.Last();
        var button = page.Find($"Button {debugButtons}").gameObject;
        var btnController = button.gameObject.AddComponent<ButtonController>();
        btnController.OnPressed += onPress;
        btnController.SetText(title);
        buttons.Add(btnController);
        debugButtons++;
    }

    public void PreviousPage(ButtonController button, bool isPressed)
    {
        button.IsPressed = false;
        pageIndex--;
        for (var i = 0; i < modPages.Count; i++) modPages[i].gameObject.SetActive(i == pageIndex);
        modPage = modPages[pageIndex].gameObject;
    }

    public void NextPage(ButtonController button, bool isPressed)
    {
        button.IsPressed = false;
        pageIndex++;
        for (var i = 0; i < modPages.Count; i++) modPages[i].gameObject.SetActive(i == pageIndex);
        modPage = modPages[pageIndex].gameObject;
    }

    public void SetupInteraction()
    {
        throwOnDetach = true;
        priority = 100;
        OnSelectExit += (_, __) =>
        {
            AddBlockerToAllButtons(ButtonController.Blocker.MENU_FALLING);
            docked = false;
        };
        OnSelectEnter += (_, __) => { RemoveBlockerFromAllButtons(ButtonController.Blocker.MENU_FALLING); };
    }

    public Material GetMaterial(string name)
    {
        foreach (var renderer in FindObjectsOfType<Renderer>())
        {
            var _name = renderer.material.name.ToLower();
            if (_name.Contains(name)) return renderer.material;
        }

        return null;
    }

    public void AddBlockerToAllButtons(ButtonController.Blocker blocker)
    {
        foreach (var button in buttons) button.AddBlocker(blocker);
    }

    public void RemoveBlockerFromAllButtons(ButtonController.Blocker blocker)
    {
        foreach (var button in buttons) button.RemoveBlocker(blocker);
    }

    public static void BindConfigEntries()
    {
        try
        {
            var inputDesc = new ConfigDescription(
                "Which button you press to open the menu",
                new AcceptableValueList<string>("gesture", "stick", "a/x", "b/y")
            );
            SummonInput = Plugin.configFile.Bind("General",
                "open menu",
                "gesture",
                inputDesc
            );

            var handDesc = new ConfigDescription(
                "Which hand can open the menu",
                new AcceptableValueList<string>("left", "right")
            );
            SummonInputHand = Plugin.configFile.Bind("General",
                "open hand",
                "right",
                handDesc
            );

            var ThemeDesc = new ConfigDescription(
                "Which Theme Should Grate Use?",
                new AcceptableValueList<string>("grate", "OldGrate", "bark", "holowpurple", "shinyrocks", "Player")
            );
            Theme = Plugin.configFile.Bind("General",
                "theme",
                "Grate",
                ThemeDesc
            );
            var FestiveDesc = new ConfigDescription(
                "Should the christmas lights be on?",
                new AcceptableValueList<bool>(true, false)
            );
            Festive = Plugin.configFile.Bind("General",
                "festive",
                false,
                FestiveDesc
            );
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }
}