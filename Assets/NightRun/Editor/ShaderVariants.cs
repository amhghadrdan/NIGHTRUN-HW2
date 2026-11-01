using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Remove unsupported game features from dynamically-created material variants.</summary>
public sealed class ShaderVariants : IPreprocessShaders
{
    public int callbackOrder => 0;
    static readonly HashSet<string> Unused = new HashSet<string>
    {
        "LIGHTMAP_ON", "DYNAMICLIGHTMAP_ON", "DIRLIGHTMAP_COMBINED", "LIGHTMAP_SHADOW_MIXING", "SHADOWS_SHADOWMASK",
        "STEREO_INSTANCING_ON", "STEREO_MULTIVIEW_ON", "STEREO_INSTANCING_ENABLED", "STEREO_DOUBLEWIDE_TARGET",
        "_PARALLAXMAP", "_DETAIL_MULX2", "_METALLICGLOSSMAP", "_SPECGLOSSMAP", "_NORMALMAP",
        "_ALPHATEST_ON", "_ALPHABLEND_ON", "_ALPHAPREMULTIPLY_ON", "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A"
    };
    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (shader.name != "Standard" && !shader.name.StartsWith("NightRun/") && !shader.name.StartsWith("Hidden/PostProcessing/")) return;
        for (int i = data.Count - 1; i >= 0; i--)
            foreach (var keyword in data[i].shaderKeywordSet.GetShaderKeywords())
                if (Unused.Contains(keyword.name)) { data.RemoveAt(i); break; }
    }
}
