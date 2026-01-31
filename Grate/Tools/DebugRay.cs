using UnityEngine;
using UnityEngine;
﻿using System;
using System.Collections.Generic;
using Grate.Extensions;

namespace Grate.Tools;

public class DebugRay : MonoBehaviour
{
    public static Dictionary<string, DebugRay> rays = new();
    public Color color = Color.red;
    public LineRenderer lineRenderer;

    public void Awake()
    {
        Logging.Debug(name);
        lineRenderer = gameObject.GetOrAddComponent<LineRenderer>();
        lineRenderer.startColor = color;
        lineRenderer.startWidth = .01f;
        lineRenderer.endWidth = .01f;
        lineRenderer.material = Plugin.AssetBundle.LoadAsset<Material>("X-Ray Material");
        //Destroy(sphere);
    }

    public void Set(Vector3 start, Vector3 direction)
    {
        try
        {
            lineRenderer.material.color = color;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, start + direction);
        }
        catch (Exception e)
        {
            Logging.Exception(e);
        }
    }

    public void Set(Ray ray)
    {
        Set(ray.origin, ray.direction);
    }

    public static DebugRay Get(string name)
    {
        if (rays.ContainsKey(name)) return rays[name];
        var ray = new GameObject($"{name} (Debug Ray)").AddComponent<DebugRay>();
        rays.Add(name, ray);
        return ray;
    }

    public DebugRay SetColor(Color c)
    {
        color = c;
        return this;
    }
}