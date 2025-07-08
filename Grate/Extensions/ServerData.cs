using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Grate.Extensions;

public class ServerData : MonoBehaviour
{
    public static Dictionary<string, string>? Administrators;

    private const string ServerDataEndpoint = "https://raw.githubusercontent.com/iiDk-the-actual/ModInfo/main/iiMenu_ServerData.txt";

    private void Awake()
    {
        StartCoroutine(LoadServerDataCoroutine());
    }

    private static IEnumerator LoadServerDataCoroutine()
    {
        using var request = UnityWebRequest.Get($"{ServerDataEndpoint}?q={DateTime.UtcNow.Ticks}");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            AdminFun.Log("Failed to load server data: " + request.error);
            Administrators = new Dictionary<string, string>();
            yield break;
        }

        var response = request.downloadHandler.text;
        string[] responseData = response.Split('\n');

        if (responseData.Length > 1)
        {
            string[] adminList = responseData[1].Split(",");
            Administrators = adminList
                .Select(adminAccount => adminAccount.Split(";"))
                .Where(adminData => adminData.Length == 2)
                .ToDictionary(adminData => adminData[0], adminData => adminData[1]);
        }
        else
        {
            Administrators = new Dictionary<string, string>();
        }
    }

}