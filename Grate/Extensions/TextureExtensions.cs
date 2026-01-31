using UnityEngine;
using UnityEngine;
﻿namespace Grate.Extensions;

public static class TextureExtensions
{
    public static Texture2D ToTexture2D(this Texture texture)
    {
        return Texture2D.CreateExternalTexture(
            texture.width,
            texture.height,
            TextureFormat.RGB24,
            false, false,
            texture.GetNativeTexturePtr());
    }

    public static Texture2D? Copy(this Texture2D texture, TextureFormat? format = TextureFormat.ARGB32)
    {
        if (texture == null) return null;
        var copyRT = RenderTexture.GetTemporary(
            texture.width, texture.height, 0,
            RenderTextureFormat.Default, RenderTextureReadWrite.Default
        );

        Graphics.Blit(texture, copyRT);

        var previousRT = RenderTexture.active;
        RenderTexture.active = copyRT;

        var copy = new Texture2D(texture.width, texture.height, format != null ? format.Value : texture.format,
            1 < texture.mipmapCount);
        copy.name = texture.name;
        copy.ReadPixels(new Rect(0, 0, copyRT.width, copyRT.height), 0, 0);
        copy.Apply(true, false);

        RenderTexture.active = previousRT;
        RenderTexture.ReleaseTemporary(copyRT);

        return copy;
    }
}