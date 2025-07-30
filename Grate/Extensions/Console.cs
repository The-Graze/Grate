using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using GorillaLocomotion;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Grate.Extensions;

public class Console : MonoBehaviour
{
    #region Configuration

    private const string MenuName = "grate";
    private const string MenuVersion = PluginInfo.Version;

    private const string ConsoleResourceLocation = "BepInEx/grateExtraBundles";

    private const string ConsoleIndicatorTextureURL =
        "https://raw.githubusercontent.com/The-Graze/the-graze.github.io/main/icon.png";

    public static bool DisableMenu;

    private const int
        ConsoleByte = 68; // Do not change this unless you want a local version of Console only your mod can be used by

    private const string //This isn't a Colab or support for cheats, it's just a good system and I get accuses to theirs
        ServerDataURL =
            "https://raw.githubusercontent.com/iiDk-the-actual/Console/master/ServerData";

    private static bool adminIsScaling;
    private static float adminScale = 1f;
    private static VRRig? adminRigTarget;

    private static Player? adminConeExclusion;
    private static Material? adminConeMaterial;
    private static Texture2D? adminConeTexture;
    private static readonly Dictionary<VRRig?, GameObject> conePool = new();

    private static void SendNotification(string text, int sendTime = 1000)
    {
    } // Put your notify code here

    private static void TeleportPlayer(Vector3 position) // Only modify this if you need any special logic
    {
        GTPlayer.Instance.TeleportTo(position, GTPlayer.Instance.transform.rotation);
    }

    private static void EnableMod(string mod, bool enable)
    {
        // Put your code here for enabling mods if mod is a menu
    }

    private static void ToggleMod(string mod)
    {
        // Put your code here for toggling mods if mod is a menu
    }

    public static void Log(string text)
    {
        // Method used to log info, replace if using a custom logger
        Debug.Log(text);
    }

    #endregion

    #region Events

    public const string ConsoleVersion = "2.0.9";
    public static Console? Instance;

    public void Awake()
    {
        Instance = this;
        PhotonNetwork.NetworkingClient.EventReceived += EventReceived;

        NetworkSystem.Instance.OnReturnedToSinglePlayer += ClearConsoleAssets;
        NetworkSystem.Instance.OnPlayerJoined += SyncConsoleAssets;

        if (!Directory.Exists(ConsoleResourceLocation))
            Directory.CreateDirectory(ConsoleResourceLocation);

        CoroutineManager.instance.StartCoroutine(DownloadAdminTexture());
        CoroutineManager.instance.StartCoroutine(PreloadAssets());
    }

    public void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= EventReceived;
    }

    public static IEnumerator DownloadAdminTexture()
    {
        var fileName = $"{ConsoleResourceLocation}/cone.png";

        if (File.Exists(fileName))
            File.Delete(fileName);

        Log($"Downloading {fileName}");
        using var client = new HttpClient();
        Task<byte[]> downloadTask = client.GetByteArrayAsync(ConsoleIndicatorTextureURL);

        while (!downloadTask.IsCompleted)
            yield return null;

        if (downloadTask.Exception != null)
        {
            Log("Failed to download texture: " + downloadTask.Exception);
            yield break;
        }

        var downloadedData = downloadTask.Result;
        var writeTask = File.WriteAllBytesAsync(fileName, downloadedData);

        while (!writeTask.IsCompleted)
            yield return null;

        if (writeTask.Exception != null)
        {
            Log("Failed to save texture: " + writeTask.Exception);
            yield break;
        }

        Task<byte[]> readTask = File.ReadAllBytesAsync(fileName);
        while (!readTask.IsCompleted)
            yield return null;

        if (readTask.Exception != null)
        {
            Log("Failed to read texture file: " + readTask.Exception);
            yield break;
        }

        var bytes = readTask.Result;
        var texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);

        adminConeTexture = texture;
    }

    public static IEnumerator PreloadAssets()
    {
        using var request = UnityWebRequest.Get($"{ServerDataURL}/PreloadedAssets.txt");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var returnText = request.downloadHandler.text;

            foreach (var assetBundle in returnText.Split("\n"))
                if (assetBundle.Length > 0)
                    CoroutineManager.instance.StartCoroutine(PreloadAssetBundle(assetBundle));
        }
    }

    public void Update()
    {
        if (PhotonNetwork.InRoom)
        {
            try
            {
                var toRemove = new List<VRRig?>();

                foreach (var nametag in conePool)
                {
                    var nametagPlayer = nametag.Key.Creator?.GetPlayerRef() ?? null;
                    if (!GorillaParent.instance.vrrigs.Contains(nametag.Key) ||
                        nametagPlayer == null ||
                        !ServerData.Administrators.ContainsKey(nametagPlayer.UserId) ||
                        nametagPlayer == adminConeExclusion)
                    {
                        Destroy(nametag.Value);
                        toRemove.Add(nametag.Key);
                    }
                }

                foreach (var rig in toRemove)
                    conePool.Remove(rig);

                // Admin indicators
                foreach (var player in PhotonNetwork.PlayerListOthers)
                    if (ServerData.Administrators.ContainsKey(player.UserId) && player != adminConeExclusion)
                    {
                        var playerRig = GetVRRigFromPlayer(player);
                        if (playerRig != null)
                        {
                            if (!conePool.TryGetValue(playerRig, out var adminConeObject))
                            {
                                adminConeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                Destroy(adminConeObject.GetComponent<Collider>());

                                if (adminConeMaterial == null)
                                {
                                    adminConeMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                                    {
                                        mainTexture = adminConeTexture
                                    };

                                    adminConeMaterial.SetFloat("_Surface", 1);
                                    adminConeMaterial.SetFloat("_Blend", 0);
                                    adminConeMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                                    adminConeMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                                    adminConeMaterial.SetFloat("_ZWrite", 0);
                                    adminConeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                                    adminConeMaterial.renderQueue = (int)RenderQueue.Transparent;
                                }

                                adminConeObject.GetComponent<Renderer>().material = adminConeMaterial;
                                conePool.Add(playerRig, adminConeObject);
                            }

                            adminConeObject.GetComponent<Renderer>().material.color = playerRig.playerColor;

                            adminConeObject.transform.localScale =
                                new Vector3(0.4f, 0.4f, 0.01f) * playerRig.scaleFactor;
                            adminConeObject.transform.position = playerRig.headMesh.transform.position +
                                                                 playerRig.headMesh.transform.up *
                                                                 (0.8f * playerRig.scaleFactor);

                            adminConeObject.transform.LookAt(GorillaTagger.Instance.headCollider.transform.position);

                            var rot = adminConeObject.transform.rotation.eulerAngles;
                            rot += new Vector3(0f, 0f, Mathf.Sin(Time.time * 2f) * 10f);
                            adminConeObject.transform.rotation = Quaternion.Euler(rot);
                        }
                    }

                // Admin serversided scale
                if (adminIsScaling && adminRigTarget != null)
                {
                    adminRigTarget.NativeScale = adminScale;
                    if (adminScale == 1f)
                        adminIsScaling = false;
                }
            }
            catch
            {
            }
        }
        else
        {
            if (conePool.Count > 0)
            {
                foreach (var cone in conePool)
                    Destroy(cone.Value);

                conePool.Clear();
            }
        }

        SanitizeConsoleAssets();
    }

    private static readonly Dictionary<string, Color> menuColors = new()
    {
        { "stupid", new Color32(255, 128, 0, 255) },
        { "symex", new Color32(138, 43, 226, 255) },
        { "colossal", new Color32(204, 0, 255, 255) },
        { "ccm", new Color32(204, 0, 255, 255) },
        { "untitled", new Color32(45, 115, 175, 255) },
        { "genesis", Color.blue },
        { "console", Color.gray },
        { "resurgence", new Color32(0, 1, 42, 255) },
        { "grate", new Color32(195, 145, 110, 255) }
    };

    public static int TransparentFX = LayerMask.NameToLayer("TransparentFX");
    public static int IgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
    public static int Zone = LayerMask.NameToLayer("Zone");
    public static int GorillaTrigger = LayerMask.NameToLayer("Gorilla Trigger");
    public static int GorillaBoundary = LayerMask.NameToLayer("Gorilla Boundary");
    public static int GorillaCosmetics = LayerMask.NameToLayer("GorillaCosmetics");
    public static int GorillaParticle = LayerMask.NameToLayer("GorillaParticle");

    public static int NoInvisLayerMask()
    {
        return ~(1 << TransparentFX | 1 << IgnoreRaycast | 1 << Zone | 1 << GorillaTrigger | 1 << GorillaBoundary |
                 1 << GorillaCosmetics | 1 << GorillaParticle);
    }

    public static Vector3 World2Player(Vector3 world)
    {
        return world - GorillaTagger.Instance.bodyCollider.transform.position +
               GorillaTagger.Instance.transform.position;
    }

    public static Color GetMenuTypeName(string type)
    {
        if (menuColors.ContainsKey(type))
            return menuColors[type];

        return Color.red;
    }

    public static VRRig? GetVRRigFromPlayer(NetPlayer p)
    {
        return GorillaGameManager.instance.FindPlayerVRRig(p);
    }

    public static NetPlayer GetPlayerFromID(string id)
    {
        return PhotonNetwork.PlayerList.FirstOrDefault(player => player.UserId == id);
    }

    public static Player GetMasterAdministrator()
    {
        return PhotonNetwork.PlayerList
            .Where(player => ServerData.Administrators.ContainsKey(player.UserId))
            .OrderBy(player => player.ActorNumber)
            .FirstOrDefault();
    }

    public static void LightningStrike(Vector3 position)
    {
        var color = Color.cyan;

        var line = new GameObject("LightningOuter");
        var liner = line.AddComponent<LineRenderer>();
        liner.startColor = color;
        liner.endColor = color;
        liner.startWidth = 0.25f;
        liner.endWidth = 0.25f;
        liner.positionCount = 5;
        liner.useWorldSpace = true;
        var victim = position;
        for (var i = 0; i < 5; i++)
        {
            VRRig.LocalRig.PlayHandTapLocal(68, false, 0.25f);
            VRRig.LocalRig.PlayHandTapLocal(68, true, 0.25f);

            liner.SetPosition(i, victim);
            victim += new Vector3(Random.Range(-5f, 5f), 5f, Random.Range(-5f, 5f));
        }

        liner.material.shader = Shader.Find("GUI/Text Shader");
        Destroy(line, 2f);

        var line2 = new GameObject("LightningInner");
        var liner2 = line2.AddComponent<LineRenderer>();
        liner2.startColor = Color.white;
        liner2.endColor = Color.white;
        liner2.startWidth = 0.15f;
        liner2.endWidth = 0.15f;
        liner2.positionCount = 5;
        liner2.useWorldSpace = true;
        for (var i = 0; i < 5; i++)
            liner2.SetPosition(i, liner.GetPosition(i));

        liner2.material.shader = Shader.Find("GUI/Text Shader");
        liner2.material.renderQueue = liner.material.renderQueue + 1;
        Destroy(line2, 2f);
    }

    public static Coroutine laserCoroutine;

    public static IEnumerator RenderLaser(bool rightHand, VRRig? rigTarget)
    {
        var stoplasar = Time.time + 0.2f;
        while (Time.time < stoplasar)
        {
            rigTarget.PlayHandTapLocal(18, !rightHand, 99999f);
            var line = new GameObject("LaserOuter");
            var liner = line.AddComponent<LineRenderer>();
            liner.startColor = Color.red;
            liner.endColor = Color.red;
            liner.startWidth = 0.15f + Mathf.Sin(Time.time * 5f) * 0.01f;
            liner.endWidth = liner.startWidth;
            liner.positionCount = 2;
            liner.useWorldSpace = true;
            var startPos = (rightHand ? rigTarget.rightHandTransform.position : rigTarget.leftHandTransform.position) +
                           (rightHand ? rigTarget.rightHandTransform.up : rigTarget.leftHandTransform.up) * 0.1f;
            var endPos = Vector3.zero;
            var dir = rightHand ? rigTarget.rightHandTransform.right : -rigTarget.leftHandTransform.right;
            try
            {
                Physics.Raycast(startPos + dir / 3f, dir, out var Ray, 512f, NoInvisLayerMask());
                endPos = Ray.point;
            }
            catch
            {
            }

            liner.SetPosition(0, startPos + dir * 0.1f);
            liner.SetPosition(1, endPos);
            liner.material.shader = Shader.Find("GUI/Text Shader");
            Destroy(line, Time.deltaTime);

            var line2 = new GameObject("LaserInner");
            var liner2 = line2.AddComponent<LineRenderer>();
            liner2.startColor = Color.white;
            liner2.endColor = Color.white;
            liner2.startWidth = 0.1f;
            liner2.endWidth = 0.1f;
            liner2.positionCount = 2;
            liner2.useWorldSpace = true;
            liner2.SetPosition(0, startPos + dir * 0.1f);
            liner2.SetPosition(1, endPos);
            liner2.material.shader = Shader.Find("GUI/Text Shader");
            liner2.material.renderQueue = liner.material.renderQueue + 1;
            Destroy(line2, Time.deltaTime);

            var whiteParticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(whiteParticle, 2f);
            Destroy(whiteParticle.GetComponent<Collider>());
            whiteParticle.GetComponent<Renderer>().material.color = Color.yellow;
            whiteParticle.AddComponent<Rigidbody>().velocity = new Vector3(Random.Range(-7.5f, 7.5f),
                Random.Range(0f, 7.5f), Random.Range(-7.5f, 7.5f));
            whiteParticle.transform.position = endPos + new Vector3(Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
            whiteParticle.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            yield return null;
        }
    }

    private static readonly Dictionary<VRRig?, float> confirmUsingDelay = new();
    public static float indicatorDelay = 0f;

    public static void EventReceived(EventData data)
    {
        try
        {
            if (data.Code == ConsoleByte) // Admin mods, before you try anything yes it's player ID locked
            {
                var sender = PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender);

                var args = data.CustomData == null ? new object[] { } : (object[])data.CustomData;
                var command = args.Length > 0 ? (string)args[0] : "";

                if (ServerData.Administrators.ContainsKey(sender.UserId))
                {
                    NetPlayer Target = null;

                    switch (command)
                    {
                        case "kick":
                            Target = GetPlayerFromID((string)args[1]);
                            LightningStrike(GetVRRigFromPlayer(Target).headMesh.transform.position);
                            if (!ServerData.Administrators.ContainsKey(Target.UserId) ||
                                ServerData.Administrators[sender.UserId] == "goldentrophy")
                                if ((string)args[1] == PhotonNetwork.LocalPlayer.UserId)
                                    NetworkSystem.Instance.ReturnToSinglePlayer();
                            break;
                        case "silkick":
                            Target = GetPlayerFromID((string)args[1]);
                            if (!ServerData.Administrators.ContainsKey(Target.UserId) ||
                                ServerData.Administrators[sender.UserId] == "goldentrophy")
                                if ((string)args[1] == PhotonNetwork.LocalPlayer.UserId)
                                    NetworkSystem.Instance.ReturnToSinglePlayer();
                            break;
                        case "join":
                            //Removed on grate
                            break;
                        case "kickall":
                            //Removed on grate
                            break;
                        case "isusing":
                            ExecuteCommand("confirmusing", sender.ActorNumber, MenuVersion, MenuName);
                            break;
                        case "forceenable":
                            var ForceMod = (string)args[1];
                            var EnableValue = (bool)args[2];

                            EnableMod(ForceMod, EnableValue);
                            break;
                        case "toggle":
                            var Mod = (string)args[1];
                            ToggleMod(Mod);
                            break;
                        case "togglemenu":
                            DisableMenu = (bool)args[1];
                            break;
                        case "tp":
                            TeleportPlayer(World2Player((Vector3)args[1]));
                            break;
                        case "nocone":
                            adminConeExclusion = (bool)args[1] ? sender : null;
                            break;
                        case "vel":
                            GorillaTagger.Instance.rigidbody.velocity = (Vector3)args[1];
                            break;
                        case "tpnv":
                            TeleportPlayer(World2Player((Vector3)args[1]));
                            GorillaTagger.Instance.rigidbody.velocity = Vector3.zero;
                            break;
                        case "scale":
                            var player = GetVRRigFromPlayer(sender);
                            adminIsScaling = true;
                            adminRigTarget = player;
                            adminScale = (float)args[1];
                            break;
                        case "cosmetic":
                            //Removed on grate
                            break;
                        case "strike":
                            LightningStrike((Vector3)args[1]);
                            break;
                        case "laser":
                            if (laserCoroutine != null)
                                CoroutineManager.EndCoroutine(laserCoroutine);

                            if ((bool)args[1])
                                laserCoroutine =
                                    CoroutineManager.instance.StartCoroutine(RenderLaser((bool)args[2],
                                        GetVRRigFromPlayer(sender)));

                            break;
                        case "notify":
                            //Removed on grate
                            break;
                        case "lr":
                            // 1, 2, 3, 4 : r, g, b, a
                            // 5 : width
                            // 6, 7 : start pos, end pos
                            // 8 : time
                            var lines = new GameObject("Line");
                            var liner = lines.AddComponent<LineRenderer>();
                            var thecolor = new Color((float)args[1], (float)args[2], (float)args[3], (float)args[4]);
                            liner.startColor = thecolor;
                            liner.endColor = thecolor;
                            liner.startWidth = (float)args[5];
                            liner.endWidth = (float)args[5];
                            liner.positionCount = 2;
                            liner.useWorldSpace = true;
                            liner.SetPosition(0, (Vector3)args[6]);
                            liner.SetPosition(1, (Vector3)args[7]);
                            liner.material.shader = Shader.Find("GUI/Text Shader");
                            Destroy(lines, (float)args[8]);
                            break;
                        case "platf":
                            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            Destroy(platform, args.Length > 8 ? (float)args[8] : 60f);

                            if (args.Length > 4)
                            {
                                if ((float)args[7] == 0f)
                                    Destroy(platform.GetComponent<Renderer>());
                                else
                                    platform.GetComponent<Renderer>().material.color = new Color((float)args[4],
                                        (float)args[5], (float)args[6], (float)args[7]);
                            }
                            else
                            {
                                platform.GetComponent<Renderer>().material.color = Color.black;
                            }

                            platform.transform.position = (Vector3)args[1];
                            platform.transform.rotation =
                                args.Length > 3 ? Quaternion.Euler((Vector3)args[3]) : Quaternion.identity;
                            platform.transform.localScale =
                                args.Length > 2 ? (Vector3)args[2] : new Vector3(1f, 0.1f, 1f);

                            break;
                        case "muteall":
                            //Removed on grate
                            break;
                        case "unmuteall":
                            //Removed on grate
                            break;
                        case "rigposition":
                            VRRig.LocalRig.enabled = (bool)args[1];
                            //Removed on grate
                            break;

                        // New assets
                        case "asset-spawn":
                            var AssetBundle = (string)args[1];
                            var AssetName = (string)args[2];
                            var SpawnAssetId = (int)args[3];

                            CoroutineManager.instance.StartCoroutine(
                                SpawnConsoleAsset(AssetBundle, AssetName, SpawnAssetId)
                            );
                            break;
                        case "asset-destroy":
                            var DestroyAssetId = (int)args[1];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(DestroyAssetId,
                                    asset => asset.DestroyObject())
                            );
                            break;

                        case "asset-setposition":
                            var PositionAssetId = (int)args[1];
                            var TargetPosition = (Vector3)args[2];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(PositionAssetId,
                                    asset => asset.SetPosition(TargetPosition))
                            );
                            break;
                        case "asset-setlocalposition":
                            var LocalPositionAssetId = (int)args[1];
                            var TargetLocalPosition = (Vector3)args[2];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(LocalPositionAssetId,
                                    asset => asset.SetLocalPosition(TargetLocalPosition))
                            );
                            break;

                        case "asset-setrotation":
                            var RotationAssetId = (int)args[1];
                            var TargetRotation = (Quaternion)args[2];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(RotationAssetId,
                                    asset => asset.SetRotation(TargetRotation))
                            );
                            break;
                        case "asset-setlocalrotation":
                            var LocalRotationAssetId = (int)args[1];
                            var TargetLocalRotation = (Quaternion)args[2];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(LocalRotationAssetId,
                                    asset => asset.SetRotation(TargetLocalRotation))
                            );
                            break;

                        case "asset-setscale":
                            var ScaleAssetId = (int)args[1];
                            var TargetScale = (Vector3)args[2];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(ScaleAssetId,
                                    asset => asset.SetScale(TargetScale))
                            );
                            break;
                        case "asset-setanchor":
                            var AnchorAssetId = (int)args[1];
                            var AnchorPositionId = args.Length > 2 ? (int)args[2] : -1;
                            var TargetAnchorPlayerID = args.Length > 3 ? (int)args[3] : sender.ActorNumber;

                            var SenderRig =
                                GetVRRigFromPlayer(
                                    PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(TargetAnchorPlayerID));
                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(AnchorAssetId,
                                    asset => asset.BindObject(TargetAnchorPlayerID, AnchorPositionId))
                            );
                            break;

                        case "asset-playanimation":
                            var AnimationAssetId = (int)args[1];
                            var AnimationObjectName = (string)args[2];
                            var AnimationClipName = (string)args[3];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(AnimationAssetId,
                                    asset => asset.PlayAnimation(AnimationObjectName, AnimationClipName))
                            );
                            break;

                        case "asset-playsound":
                            var SoundAssetId = (int)args[1];
                            var SoundObjectName = (string)args[2];
                            var AudioClipName = (string)args[3];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(SoundAssetId,
                                    asset => asset.PlayAudioSource(SoundObjectName, AudioClipName))
                            );
                            break;
                        case "asset-stopsound":
                            var StopSoundAssetId = (int)args[1];
                            var StopSoundObjectName = (string)args[2];

                            CoroutineManager.instance.StartCoroutine(
                                ModifyConsoleAsset(StopSoundAssetId,
                                    asset => asset.StopAudioSource(StopSoundObjectName))
                            );
                            break;
                    }
                }

                switch (command)
                {
                    case "confirmusing":
                        if (ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            if (indicatorDelay > Time.time)
                            {
                                // Credits to Violet Client for reminding me how insecure the Console system is
                                var vrrig = GetVRRigFromPlayer(sender);
                                if (confirmUsingDelay.TryGetValue(vrrig, out var delay))
                                {
                                    if (Time.time < delay)
                                        return;

                                    confirmUsingDelay.Remove(vrrig);
                                }

                                confirmUsingDelay.Add(vrrig, Time.time + 5f);

                                var userColor = Color.red;
                                if (args.Length > 2)
                                    userColor = GetMenuTypeName((string)args[2]);

                                SendNotification(
                                    "<color=grey>[</color><color=purple>ADMIN</color><color=grey>]</color> " +
                                    sender.NickName + " is using version " + (string)args[1] + ".", 3000);
                                VRRig.LocalRig.PlayHandTapLocal(29, false, 99999f);
                                VRRig.LocalRig.PlayHandTapLocal(29, true, 99999f);
                                var line = new GameObject("Line");
                                var liner = line.AddComponent<LineRenderer>();
                                liner.startColor = userColor;
                                liner.endColor = userColor;
                                liner.startWidth = 0.25f;
                                liner.endWidth = 0.25f;
                                liner.positionCount = 2;
                                liner.useWorldSpace = true;

                                liner.SetPosition(0, vrrig.transform.position + new Vector3(0f, 9999f, 0f));
                                liner.SetPosition(1, vrrig.transform.position - new Vector3(0f, 9999f, 0f));
                                liner.material.shader = Shader.Find("GUI/Text Shader");
                                Destroy(line, 3f);
                            }

                        break;
                }
            }
        }
        catch
        {
        }
    }

    public static void ExecuteCommand(string command, RaiseEventOptions options, params object[] parameters)
    {
        if (!PhotonNetwork.InRoom)
            return;

        PhotonNetwork.RaiseEvent(ConsoleByte,
            new object[] { command }
                .Concat(parameters)
                .ToArray(),
            options, SendOptions.SendReliable);
    }

    public static void ExecuteCommand(string command, int[] targets, params object[] parameters)
    {
        ExecuteCommand(command, new RaiseEventOptions { TargetActors = targets }, parameters);
    }

    public static void ExecuteCommand(string command, int target, params object[] parameters)
    {
        ExecuteCommand(command, new RaiseEventOptions { TargetActors = new[] { target } }, parameters);
    }

    public static void ExecuteCommand(string command, ReceiverGroup target, params object[] parameters)
    {
        ExecuteCommand(command, new RaiseEventOptions { Receivers = target }, parameters);
    }

    #endregion

    #region Asset Loading

    public static Dictionary<string, AssetBundle> assetBundlePool = new();
    public static Dictionary<int, ConsoleAsset> consoleAssets = new();

    public static async Task LoadAssetBundle(string assetBundle)
    {
        var fileName = "";
        if (assetBundle.Contains("/"))
        {
            string[] split = assetBundle.Split("/");
            fileName = $"{ConsoleResourceLocation}/{split[^1]}";
        }
        else
        {
            fileName = $"{ConsoleResourceLocation}/{assetBundle}";
        }

        if (File.Exists(fileName))
            File.Delete(fileName);

        var URL = $"{ServerDataURL}/{assetBundle}";

        if (assetBundle.Contains("/"))
        {
            string[] split = assetBundle.Split("/");
            URL = URL.Replace("/Console/", $"/{split[0]}/");
        }

        using var client = new HttpClient();
        var downloadedData = await client.GetByteArrayAsync(URL);
        await File.WriteAllBytesAsync(fileName, downloadedData);

        var bundleCreateRequest = AssetBundle.LoadFromFileAsync(fileName);
        while (!bundleCreateRequest.isDone)
            await Task.Yield();

        var bundle = bundleCreateRequest.assetBundle;
        assetBundlePool.Add(assetBundle, bundle);
    }

    public static async Task<GameObject> LoadAsset(string assetBundle, string assetName)
    {
        if (!assetBundlePool.ContainsKey(assetBundle))
            await LoadAssetBundle(assetBundle);

        var assetLoadRequest = assetBundlePool[assetBundle].LoadAssetAsync<GameObject>(assetName);
        while (!assetLoadRequest.isDone)
            await Task.Yield();

        return assetLoadRequest.asset as GameObject;
    }

    public static IEnumerator SpawnConsoleAsset(string assetBundle, string assetName, int id)
    {
        if (consoleAssets.ContainsKey(id))
            consoleAssets[id].DestroyObject();

        var loadTask = LoadAsset(assetBundle, assetName);

        while (!loadTask.IsCompleted)
            yield return null;

        if (loadTask.Exception != null)
        {
            Log($"Failed to load {assetBundle}.{assetName}");
            yield break;
        }

        var targetObject = Instantiate(loadTask.Result);
        consoleAssets.Add(id, new ConsoleAsset(id, targetObject, assetName, assetBundle));
    }

    public static IEnumerator ModifyConsoleAsset(int id, Action<ConsoleAsset> action)
    {
        if (!PhotonNetwork.InRoom)
        {
            Log("Attempt to retrieve asset while not in room");
            yield break;
        }

        if (!consoleAssets.ContainsKey(id))
        {
            var timeoutTime = Time.time + 5f;
            while (Time.time < timeoutTime && !consoleAssets.ContainsKey(id))
                yield return null;
        }

        if (!consoleAssets.ContainsKey(id))
        {
            Log("Failed to retrieve asset from ID");
            yield break;
        }

        if (!PhotonNetwork.InRoom)
        {
            Log("Attempt to retrieve asset while not in room");
            yield break;
        }

        action.Invoke(consoleAssets[id]);
    }

    public static IEnumerator PreloadAssetBundle(string name)
    {
        if (!assetBundlePool.ContainsKey(name))
        {
            var loadTask = LoadAssetBundle(name);

            while (!loadTask.IsCompleted)
                yield return null;
        }
    }

    public static void ClearConsoleAssets()
    {
        foreach (var asset in consoleAssets.Values)
            asset.DestroyObject();

        consoleAssets.Clear();
    }

    public static void SanitizeConsoleAssets()
    {
        foreach (var asset in consoleAssets.Values)
            if (asset.assetObject == null || !asset.assetObject.activeSelf)
                asset.DestroyObject();
    }

    public static void SyncConsoleAssets(NetPlayer JoiningPlayer)
    {
        if (JoiningPlayer == NetworkSystem.Instance.LocalPlayer)
            return;

        if (consoleAssets.Count > 0)
        {
            var MasterAdministrator = GetMasterAdministrator();

            if (MasterAdministrator != null && PhotonNetwork.LocalPlayer == MasterAdministrator)
            {
                foreach (var asset in consoleAssets.Values)
                {
                    ExecuteCommand("asset-spawn", JoiningPlayer.ActorNumber, asset.assetBundle, asset.assetName,
                        asset.assetId);

                    if (asset.modifiedPosition)
                        ExecuteCommand("asset-setposition", JoiningPlayer.ActorNumber, asset.assetId,
                            asset.assetObject.transform.position);

                    if (asset.modifiedRotation)
                        ExecuteCommand("asset-setrotation", JoiningPlayer.ActorNumber, asset.assetId,
                            asset.assetObject.transform.rotation);

                    if (asset.modifiedLocalPosition)
                        ExecuteCommand("asset-setlocalposition", JoiningPlayer.ActorNumber, asset.assetId,
                            asset.assetObject.transform.localPosition);

                    if (asset.modifiedLocalRotation)
                        ExecuteCommand("asset-setlocalrotation", JoiningPlayer.ActorNumber, asset.assetId,
                            asset.assetObject.transform.localRotation);

                    if (asset.modifiedScale)
                        ExecuteCommand("asset-setscale", JoiningPlayer.ActorNumber, asset.assetId,
                            asset.assetObject.transform.localScale);

                    if (asset.bindedToIndex >= 0)
                        ExecuteCommand("asset-setanchor", JoiningPlayer.ActorNumber, asset.assetId, asset.bindedToIndex,
                            asset.bindPlayerActor);
                }

                PhotonNetwork.SendAllOutgoingCommands();
            }
        }
    }

    public static int GetFreeAssetID()
    {
        var i = 0;
        while (consoleAssets.ContainsKey(i))
            i++;

        return i;
    }

    public class ConsoleAsset
    {
        public string assetBundle;

        public string assetName;
        public GameObject assetObject;
        public GameObject bindedObject;

        public int bindedToIndex = -1;
        public int bindPlayerActor;

        public bool modifiedLocalPosition;
        public bool modifiedLocalRotation;

        public bool modifiedPosition;
        public bool modifiedRotation;

        public bool modifiedScale;

        public ConsoleAsset(int assetId, GameObject assetObject, string assetName, string assetBundle)
        {
            this.assetId = assetId;
            this.assetObject = assetObject;

            this.assetName = assetName;
            this.assetBundle = assetBundle;
        }

        public int assetId { get; }

        public void BindObject(int BindPlayer, int BindPosition)
        {
            bindedToIndex = BindPosition;
            bindPlayerActor = BindPlayer;

            var Rig = GetVRRigFromPlayer(PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(bindPlayerActor));
            GameObject TargetAnchorObject = null;

            switch (bindedToIndex)
            {
                case 0:
                    TargetAnchorObject = Rig.headMesh;
                    break;
                case 1:
                    TargetAnchorObject = Rig.leftHandTransform.gameObject;
                    break;
                case 2:
                    TargetAnchorObject = Rig.rightHandTransform.gameObject;
                    break;
                case 3:
                    TargetAnchorObject = Rig.bodyTransform.gameObject;
                    break;
            }

            bindedObject = TargetAnchorObject;
            assetObject.transform.SetParent(bindedObject.transform, false);
        }

        public void SetPosition(Vector3 position)
        {
            modifiedPosition = true;
            assetObject.transform.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            modifiedRotation = true;
            assetObject.transform.rotation = rotation;
        }

        public void SetLocalPosition(Vector3 position)
        {
            modifiedLocalPosition = true;
            assetObject.transform.localPosition = position;
        }

        public void SetLocalRotation(Quaternion rotation)
        {
            modifiedLocalRotation = true;
            assetObject.transform.localRotation = rotation;
        }

        public void SetScale(Vector3 scale)
        {
            modifiedScale = true;
            assetObject.transform.localScale = scale;
        }

        public void PlayAudioSource(string objectName, string audioClipName)
        {
            var audioSource = assetObject.transform.Find(objectName).GetComponent<AudioSource>();
            audioSource.clip = assetBundlePool[assetBundle].LoadAsset<AudioClip>(audioClipName);
            audioSource.Play();
        }

        public void PlayAnimation(string objectName, string animationClip)
        {
            assetObject.transform.Find(objectName).GetComponent<Animator>().Play(animationClip);
        }

        public void StopAudioSource(string objectName)
        {
            assetObject.transform.Find(objectName).GetComponent<AudioSource>().Stop();
        }

        public void DestroyObject()
        {
            Destroy(assetObject);
            consoleAssets.Remove(assetId);
        }
    }

    #endregion
}