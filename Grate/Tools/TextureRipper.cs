using System;
using System.Collections.Generic;
using System.IO;
using Grate.Extensions;
using UnityEngine;

namespace Grate.Tools;

public static class TextureRipper
{
    public static string folderName = "C:\\Users\\ultra\\Pictures\\Gorilla Tag Textures";

    public static void Rip()
    {
        var step = "Start";
        Directory.CreateDirectory(folderName);
        try
        {
            step = "Locating renderers";
            var renderers = GameObject.FindObjectsOfType<Renderer>();
            Logging.Debug("Found", renderers.Length, "renderers");
            step = "Looping through renderers";
            var knownTextures = new List<Texture>();
            foreach (var renderer in renderers)
            {
                step = "Formatting file path";
                step = "Storing materials";
                Material[] materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                    try
                    {
                        step = "Getting main texutre";
                        var texture = materials[i].mainTexture;
                        if (texture != null && !knownTextures.Contains(texture))
                        {
                            knownTextures.Add(texture);
                            step = "Creating directory";
                            step = "Encoding to png";
                            var bytes = (texture as Texture2D).Copy().EncodeToPNG();
                            step = "Getting material name";
                            var materialName = materials[i].name;
                            step = "Getting file name";
                            var filename = Path.Combine(folderName,
                                renderer.gameObject.name + "--" + materialName + ".png");
                            step = "Writing bytes";
                            if (filename.Contains("plastickey")) continue;
                            Logging.Debug(filename, bytes);
                            File.WriteAllBytes(filename, bytes);
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.Exception(e);
                    }
            }
        }
        catch (Exception e)
        {
            Logging.Warning("Failed at step", step);
            Logging.Exception(e);
        }
    }
}