using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Grate.Gestures;
using Grate.Modules;
using Grate.Modules.Movement;
using Grate.Modules.Physics;
using Grate.Modules.Multiplayer;
using Grate.Modules.Teleportation;
using Grate.Tools;
using Grate.Interaction;
using Grate.Extensions;
using Player = GorillaLocomotion.GTPlayer;
using BepInEx.Configuration;
using UnityEngine.XR;
using Grate.Modules.Misc;
using Photon.Pun;
using UnityEngine.InputSystem;
using System.Threading;
using System.Collections;
using UnityEngine.Networking;
using PlayFab.ClientModels;
using UnityEngine.XR.Interaction.Toolkit;

namespace Grate.GUI
{
    public class MenuController : GrateGrabbable
    {
        public static MenuController Instance;
        public bool Built { get; private set; }
        public Vector3
        initialMenuOffset = new Vector3(0, .035f, .65f),
        btnDimensions = new Vector3(.3f, .05f, .05f);
        public Rigidbody _rigidbody;
        public List<Transform> modPages;
        public List<ButtonController> buttons;
        public List<GrateModule> modules = new List<GrateModule>();
        public GameObject modPage, settingsPage;
        public Text helpText;
        public static InputTracker SummonTracker;
        public static ConfigEntry<string> SummonInput;
        public static ConfigEntry<string> SummonInputHand;
        public static ConfigEntry<string> Theme;
        public static ConfigEntry<bool> Festive;

        public Material[] grate, bark, HolloPurp, Monke, old;
        public static Material[] ShinyRocks;

        bool docked;

        protected override void Awake()
        {
            if (NetworkSystem.Instance.GameModeString.Contains("MODDED_"))
            {
                Instance = this;
                try
                {
                    Logging.Debug("Awake");
                    base.Awake();
                    this.throwOnDetach = true;
                    gameObject.AddComponent<PositionValidator>();
                    Plugin.configFile.SettingChanged += SettingsChanged;
                    List<GrateModule> TooAddmodules = new List<GrateModule>()
                    {
                        // Locomotion
                        gameObject.AddComponent<Airplane>(),
                        gameObject.AddComponent<Helicopter>(),
                        gameObject.AddComponent<Bubble>(),
                        gameObject.AddComponent<Fly>(),
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
                        gameObject.AddComponent<Kamehameha>(),

                        //// Misc
                        //gameObject.AddComponent<ReturnToVS>(),
                        //gameObject.AddComponent<Lobby>(),

                    };
                    CatMeow meow = gameObject.AddComponent<CatMeow>();
                    if (NetworkSystem.Instance.LocalPlayer.UserId == "FBE3EE50747CB892")
                    {
                        modules.Add(meow);
                    }
                    StoneBroke sb = gameObject.AddComponent<StoneBroke>();
                    if (NetworkSystem.Instance.LocalPlayer.UserId == "CA8FDFF42B7A1836")
                    {
                        modules.Add(sb);
                    }
                    BagHammer bs = gameObject.AddComponent<BagHammer>();
                    if (NetworkSystem.Instance.LocalPlayer.UserId == "9ABD0C174289F58E")
                    {
                        modules.Add(bs);
                    }
                    Grazing g = gameObject.AddComponent<Grazing>();
                    if (NetworkSystem.Instance.LocalPlayer.UserId == "42D7D32651E93866")
                    {
                        modules.Add(g);
                    }
                    Cheese ch = gameObject.AddComponent<Cheese>();
                    if (NetworkSystem.Instance.LocalPlayer.UserId == "B1B20DEEEDB71C63")
                    {
                        modules.Add(ch);
                    }
                    GoudabudaHat goudabudaHat = gameObject.AddComponent<GoudabudaHat>();
                    if (NetworkSystem.Instance.LocalPlayer.UserId == "A48744B93D9A3596")
                    {
                        modules.Add(goudabudaHat);
                    }
                    modules.AddRange(TooAddmodules);
                    ReloadConfiguration();
                }
                catch (Exception e) { Logging.Exception(e); }
            }
        }
        private void Start() // sigma sigma sigma s
        {
            this.Summon();
            base.transform.SetParent(null);
            base.transform.position = Vector3.zero;
            this._rigidbody.isKinematic = false;
            this._rigidbody.useGravity = true;
            base.transform.SetParent(null);
            this.AddBlockerToAllButtons(ButtonController.Blocker.MENU_FALLING);
            this.docked = false;
        }

        private void ThemeChanged()
        {
            if (grate == null)
            {
                grate = new Material[]
                {
                    Plugin.assetBundle.LoadAsset<Material>("Zipline Rope Material"),
                    Plugin.assetBundle.LoadAsset<Material>("Metal Material")
                };
                bark = new Material[]
                {
                    Plugin.assetBundle.LoadAsset<Material>("m_Menu Outer"),
                    Plugin.assetBundle.LoadAsset<Material>("m_Menu Inner")

                };
                Material mat = Plugin.assetBundle.LoadAsset<Material>("m_TK Sparkles");
                HolloPurp = new Material[]
                {
                    Plugin.assetBundle.LoadAsset<Material>("m_TK Sparkles"),
                    Plugin.assetBundle.LoadAsset<Material>("m_TK Sparkles")
                };

                Material furr = Plugin.assetBundle.LoadAsset<Material>("Gorilla Material");
                Monke = new Material[]
                {
                    furr,
                    furr
                };
                Material plain = new Material(furr);
                Material plain2 = new Material(furr);
                old = new Material[]
                {
                    plain,
                    plain2,
                };
                old[0].mainTexture = null;
                old[0].color = new Color(0.17f, 0.17f, 0.17f);
                old[1].mainTexture = null;
                old[1].color = new Color(0.2f, 0.2f, 0.2f);
            }
            string ThemeName = Theme.Value.ToLower();
            if (ThemeName == "grate")
            {
                gameObject.GetComponent<MeshRenderer>().materials = grate;
            }
            if (ThemeName == "bark")
            {
                gameObject.GetComponent<MeshRenderer>().materials = bark;
            }
            if (ThemeName == "holowpurple")
            {
                gameObject.GetComponent<MeshRenderer>().materials = HolloPurp;
            }

            if (ThemeName == "oldgrate")
            {
                gameObject.GetComponent<MeshRenderer>().materials = old;
            }

            if (ThemeName == "shinyrocks")
            {
                gameObject.GetComponent<MeshRenderer>().materials = ShinyRocks;
            }

            if (ThemeName == "player")
            {
                if (VRRig.LocalRig.CurrentCosmeticSkin != null)
                {
                    Material[] Skinned = new Material[]
                    {
                        VRRig.LocalRig.CurrentCosmeticSkin.scoreboardMaterial,
                        VRRig.LocalRig.CurrentCosmeticSkin.scoreboardMaterial
                    };
                    gameObject.GetComponent<MeshRenderer>().materials = Skinned;
                }
                else
                {
                    gameObject.GetComponent<MeshRenderer>().materials = Monke;
                    Monke[0].color = VRRig.LocalRig.playerColor;
                    Monke[1].color = VRRig.LocalRig.playerColor;
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
                ? XRNode.LeftHand : XRNode.RightHand;

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

        void SettingsChanged(object sender, SettingChangedEventArgs e)
        {
            if (e.ChangedSetting == SummonInput || e.ChangedSetting == SummonInputHand)
            {
                ReloadConfiguration();
            }
            if (e.ChangedSetting == Theme || e.ChangedSetting == Festive)
            {
                ThemeChanged();
            }
        }

        void Summon(InputTracker _) { Summon(); }

        public void Summon()
        {
            if (!Built)
                BuildMenu();
            else
                ResetPosition();
        }

        void FixedUpdate()
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
            // The potions tutorial needs to be updated frequently to keep the current size
            // up-to-date, even when the mod is disabled
            if (GrateModule.LastEnabled && GrateModule.LastEnabled == Potions.Instance)
            {
                helpText.text = Potions.Instance.Tutorial();
            }
        }

        void ResetPosition()
        {
            _rigidbody.isKinematic = true;
            _rigidbody.velocity = Vector3.zero;
            transform.SetParent(Player.Instance.bodyCollider.transform);
            transform.localPosition = initialMenuOffset;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            foreach (var button in buttons)
            {
                button.RemoveBlocker(ButtonController.Blocker.MENU_FALLING);
            }
            docked = true;
        }

        IEnumerator VerCheck()
        {
            using (UnityWebRequest request = UnityWebRequest.Get("https://raw.githubusercontent.com/The-Graze/Grate/master/ver.txt"))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string fileContents = request.downloadHandler.text;

                    Version checkedV = new Version(fileContents);
                    Version localv = new Version(PluginInfo.Version);

                    if (checkedV > localv)
                    {
                        this.gameObject.transform.Find("Version Canvas").GetComponentInChildren<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
                        this.gameObject.transform.Find("Version Canvas").GetComponentInChildren<Text>().verticalOverflow = VerticalWrapMode.Overflow;
                        this.gameObject.transform.Find("Version Canvas").GetComponentInChildren<Text>().text =
                        $"!!Update Needed!! \n GoTo: \n https://graze.cc/grate";
                    }
                    else
                    {
                        this.gameObject.transform.Find("Version Canvas").GetComponentInChildren<Text>().text =
                        $"{PluginInfo.Name} {PluginInfo.Version}";
                    }
                }
            }
        }

        void BuildMenu()
        {
            Logging.Debug("Building menu...");
            try
            {

                helpText = this.gameObject.transform.Find("Help Canvas").GetComponentInChildren<Text>();
                helpText.text = "Enable a module to see its tutorial.";
                StartCoroutine(VerCheck());
                var collider = this.gameObject.GetOrAddComponent<BoxCollider>();
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
            catch (Exception ex) { Logging.Warning(ex.Message); Logging.Warning(ex.StackTrace); return; }
            Built = true;
        }

        private void SetupSettingsPage()
        {
            GameObject button = this.gameObject.transform.Find("Settings Button").gameObject;
            ButtonController btnController = button.AddComponent<ButtonController>();
            buttons.Add(btnController);
            btnController.OnPressed += (obj, pressed) =>
            {
                settingsPage.SetActive(pressed);
                if (pressed)
                    settingsPage.GetComponent<SettingsPage>().UpdateText();
                modPage.SetActive(!pressed);
            };

            settingsPage = this.transform.Find("Settings Page").gameObject;
            settingsPage.AddComponent<SettingsPage>();
            settingsPage.SetActive(false);
        }

        public static bool debugger = true;
        public void SetupModPages()
        {
            var modPageTemplate = this.gameObject.transform.Find("Mod Page");
            int buttonsPerPage = modPageTemplate.childCount - 2; // Excludes the prev/next page btns
            int numPages = ((modules.Count - 1) / buttonsPerPage) + 1;
            if (Plugin.DebugMode)
                numPages++;

            modPages = new List<Transform>() { modPageTemplate };
            for (int i = 0; i < numPages - 1; i++)
                modPages.Add(Instantiate(modPageTemplate, this.gameObject.transform));

            buttons = new List<ButtonController>();
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];

                var page = modPages[i / buttonsPerPage];
                var button = page.Find($"Button {i % buttonsPerPage}").gameObject;

                ButtonController btnController = button.AddComponent<ButtonController>();
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

            foreach (Transform modPage in modPages)
            {
                foreach (Transform button in modPage)
                {
                    if (button.name == "Button Left" && modPage != modPages[0])
                    {
                        var btnController = button.gameObject.AddComponent<ButtonController>();
                        btnController.OnPressed += PreviousPage;
                        btnController.SetText("Prev Page");
                        buttons.Add(btnController);
                        continue;
                    }
                    else if (button.name == "Button Right" && modPage != modPages[modPages.Count - 1])
                    {
                        var btnController = button.gameObject.AddComponent<ButtonController>();
                        btnController.OnPressed += NextPage;
                        btnController.SetText("Next Page");
                        buttons.Add(btnController);
                        continue;
                    }
                    else if (!button.GetComponent<ButtonController>())
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
                {
                    Application.Quit();
                }
                else
                {
                    btn.text.text = "You sure?";
                }
            });

            AddDebugButton("Show Colliders", (btn, isPressed) =>
            {
                if (isPressed)
                {
                    foreach (var c in FindObjectsOfType<Collider>())
                        c.gameObject.AddComponent<ColliderRenderer>();
                }
                else
                {
                    foreach (var c in FindObjectsOfType<ColliderRenderer>())
                        c.Obliterate();
                }
            });
        }

        int debugButtons = 0;
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

        private int pageIndex = 0;
        public void PreviousPage(ButtonController button, bool isPressed)
        {
            button.IsPressed = false;
            pageIndex--;
            for (int i = 0; i < modPages.Count; i++)
            {
                modPages[i].gameObject.SetActive(i == pageIndex);
            }
            modPage = modPages[pageIndex].gameObject;
        }
        public void NextPage(ButtonController button, bool isPressed)
        {
            button.IsPressed = false;
            pageIndex++;
            for (int i = 0; i < modPages.Count; i++)
            {
                modPages[i].gameObject.SetActive(i == pageIndex);
            }
            modPage = modPages[pageIndex].gameObject;
        }

        public void SetupInteraction()
        {
            this.throwOnDetach = true;
            this.priority = 100;
            this.OnSelectExit += (_, __) =>
            {
                AddBlockerToAllButtons(ButtonController.Blocker.MENU_FALLING);
                docked = false;
            };
            this.OnSelectEnter += (_, __) =>
            {
                RemoveBlockerFromAllButtons(ButtonController.Blocker.MENU_FALLING);
            };

        }

        public Material GetMaterial(string name)
        {
            foreach (var renderer in FindObjectsOfType<Renderer>())
            {
                string _name = renderer.material.name.ToLower();
                if (_name.Contains(name))
                {
                    return renderer.material;
                }
            }
            return null;
        }

        public void AddBlockerToAllButtons(ButtonController.Blocker blocker)
        {
            foreach (var button in buttons)
            {
                button.AddBlocker(blocker);
            }
        }

        public void RemoveBlockerFromAllButtons(ButtonController.Blocker blocker)
        {
            foreach (var button in buttons)
            {
                button.RemoveBlocker(blocker);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Plugin.configFile.SettingChanged -= SettingsChanged;
        }

        public static void BindConfigEntries()
        {
            try
            {
                ConfigDescription inputDesc = new ConfigDescription(
                    "Which button you press to open the menu",
                    new AcceptableValueList<string>("gesture", "stick", "a/x", "b/y")
                );
                SummonInput = Plugin.configFile.Bind("General",
                    "open menu",
                    "gesture",
                    inputDesc
                );

                ConfigDescription handDesc = new ConfigDescription(
                    "Which hand can open the menu",
                    new AcceptableValueList<string>("left", "right")
                );
                SummonInputHand = Plugin.configFile.Bind("General",
                    "open hand",
                    "right",
                    handDesc
                );

                ConfigDescription ThemeDesc = new ConfigDescription(
                   "Which Theme Should Grate Use?",
                   new AcceptableValueList<string>("grate","OldGrate","bark","HolowPurple","ShinyRocks","Player")
               );
                Theme = Plugin.configFile.Bind("General",
                    "theme",
                    "Grate",
                    ThemeDesc
                );
                ConfigDescription FestiveDesc = new ConfigDescription(
                    "Should the christmas lights be on?",
                    new AcceptableValueList<bool>(true, false)
                );
                Festive = Plugin.configFile.Bind("General",
                    "festive",
                    false,
                    FestiveDesc
                );
            }
            catch (Exception e) { Logging.Exception(e); }
        }
    }
}
