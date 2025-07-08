using System.Collections;
using UnityEngine;

namespace Grate.Extensions;

public class CoroutineManager : MonoBehaviour
{
    public static CoroutineManager instance;

    private void Awake()
    {
        instance = this;
    }

    public static Coroutine? RunCoroutine(IEnumerator enumerator)
    {
        return instance.StartCoroutine(enumerator);
    }

    public static void EndCoroutine(Coroutine? enumerator)
    {
        instance.StopCoroutine(enumerator);
    }
}