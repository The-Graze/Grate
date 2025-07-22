using System.Collections.Generic;
using Grate;
using Grate.Extensions;
using UnityEngine;

public class ColliderRenderer : MonoBehaviour
{
    public float refreshRate = 10;
    private Dictionary<Transform, BoxCollider> boxColliders;
    private Dictionary<Transform, MeshCollider> meshColliders;
    private Transform obj;
    private int refreshOffset;
    private Dictionary<Transform, SphereCollider> sphereColliders;

    private void Start()
    {
        refreshOffset = Random.Range(0, 60 * (int)refreshRate);
        boxColliders = new Dictionary<Transform, BoxCollider>();
        foreach (var collider in GetComponents<BoxCollider>())
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            obj.GetComponent<BoxCollider>().Obliterate();
            var material =
                Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>("Cloud").GetComponent<Renderer>().material);
            var color = Random.ColorHSV();
            color.a = .25f;
            material.color = color;
            material.SetColor("_EmissionColor", color);
            obj.GetComponent<MeshRenderer>().material = material;
            obj.SetParent(collider.transform);
            boxColliders.Add(obj, collider);
        }

        sphereColliders = new Dictionary<Transform, SphereCollider>();
        foreach (var collider in GetComponents<SphereCollider>())
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            obj.GetComponent<SphereCollider>().Obliterate();
            var material =
                Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>("Cloud").GetComponent<Renderer>().material);
            var color = Random.ColorHSV();
            color.a = .25f;
            material.color = color;
            material.SetColor("_EmissionColor", color);
            obj.GetComponent<MeshRenderer>().material = material;
            obj.SetParent(collider.transform);
            sphereColliders.Add(obj, collider);
        }

        foreach (var collider in GetComponents<MeshCollider>())
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            obj.GetComponent<BoxCollider>().Obliterate();
            var material =
                Instantiate(Plugin.AssetBundle.LoadAsset<GameObject>("Cloud").GetComponent<Renderer>().material);
            var color = Random.ColorHSV();
            color.a = .25f;
            material.color = color;
            material.SetColor("_EmissionColor", color);
            obj.GetComponent<MeshRenderer>().material = material;
            obj.SetParent(collider.transform);
            meshColliders.Add(obj, collider);
        }

        Recalculate();
    }

    private void FixedUpdate()
    {
        if ((refreshOffset + Time.frameCount) % (60 * refreshRate) == 0) Recalculate();
    }

    private void OnDestroy()
    {
        obj?.Obliterate();
    }

    public void Recalculate()
    {
        foreach (var entry in boxColliders)
        {
            var cube = entry.Key;
            var collider = entry.Value;
            if (!collider) continue;
            cube.localPosition = collider.center;
            cube.localScale = new Vector3(
                collider.size.x,
                collider.size.y,
                collider.size.z
            );
        }

        foreach (var entry in meshColliders)
        {
            var cube = entry.Key;
            var collider = entry.Value;
            if (!collider) continue;
            cube.localPosition = collider.bounds.center;
            cube.localScale = new Vector3(
                collider.bounds.extents.x,
                collider.bounds.extents.y,
                collider.bounds.extents.z
            );
        }

        foreach (var entry in sphereColliders)
        {
            var sphere = entry.Key;
            var collider = entry.Value;
            if (!collider) continue;
            sphere.localPosition = collider.center;
            sphere.localScale = new Vector3(
                collider.radius * 2,
                collider.radius * 2,
                collider.radius * 2
            );
        }
    }
}