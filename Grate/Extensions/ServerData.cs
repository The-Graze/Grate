using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Grate.Extensions;

public class ServerData : MonoBehaviour
{
    private const string AdminDataEndpoint1 =
        "https://raw.githubusercontent.com/The-Graze/Grate/master/Admins.txt";
    private const string SupporterDataEndpoint =
        "https://raw.githubusercontent.com/The-Graze/Grate/master/supporters.txt";
    
    
    //This isn't a Colab or support for cheats, it's just a good system and I get access to theirs
    private const string AdminDataEndpoint2 =
        "https://raw.githubusercontent.com/iiDk-the-actual/ModInfo/main/iiMenu_ServerData.txt";

    public static Dictionary<string, string>? Administrators = new();
    public static Dictionary<string, string>? Supporters = new();
    private void Awake()
    {
        StartCoroutine(LoadServerDataCoroutine());
    }

    private static IEnumerator LoadServerDataCoroutine()
    {

        using var AdminRequest1 = UnityWebRequest.Get($"{AdminDataEndpoint1}");
        yield return AdminRequest1.SendWebRequest();
        
        
        if (AdminRequest1.result != UnityWebRequest.Result.Success)
        {
            Console.Log("Failed to load adminspt1: " + AdminRequest1.error);
            yield break;
        }
        var response1 = AdminRequest1.downloadHandler.text;
        var adminListPart1 = JsonConvert.DeserializeObject<Dictionary<string,string>>(response1);
        
        using var request2 = UnityWebRequest.Get($"{AdminDataEndpoint2}?q={DateTime.UtcNow.Ticks}");
        yield return request2.SendWebRequest();

        if (request2.result != UnityWebRequest.Result.Success)
        {
            Console.Log("Failed to load adminspt2: " + request2.error);
            yield break;
        }

        var response2 = request2.downloadHandler.text;
        string[] responseData = response2.Split('\n');

        if (responseData.Length <= 1) yield break;
        
        string[] adminListPt2 = responseData[1].Split(",");
        var tmp2  = adminListPt2
            .Select(adminAccount => adminAccount.Split(";"))
            .Where(adminData => adminData.Length == 2)
            .ToDictionary(adminData => adminData[0], adminData => adminData[1]);

        if (adminListPart1 != null)
            Administrators = adminListPart1
                .Concat(tmp2)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value);
        
        
        //Supporters: https://www.patreon.com/c/theGraze
        using var supporterRequest = UnityWebRequest.Get($"{SupporterDataEndpoint}");
        yield return supporterRequest.SendWebRequest();
        if (supporterRequest.result != UnityWebRequest.Result.Success)
        {
            Console.Log("Failed to load supporters: " + supporterRequest.error);
            yield break;
        }
        var response3 = supporterRequest.downloadHandler.text;
        Supporters = JsonConvert.DeserializeObject<Dictionary<string,string>>(response3);

    }
}