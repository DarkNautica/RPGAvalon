using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SyntyMaterialFixer : EditorWindow
{
    [MenuItem("DarkNautica/Fix Synty Materials (Force URP/Lit)")]
    public static void FixAllPinkMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("URP/Lit shader not found. Is URP installed?");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material");
        int converted = 0;
        int skipped = 0;
        List<string> convertedNames = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Skip if already URP
            if (mat.shader == urpLit) { skipped++; continue; }

            // Cache color/texture before swap
            Color baseColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Texture baseTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;

            mat.shader = urpLit;

            // Restore color and texture into URP properties
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_BaseMap") && baseTex != null) mat.SetTexture("_BaseMap", baseTex);

            EditorUtility.SetDirty(mat);
            converted++;
            convertedNames.Add(mat.name);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Synty Material Fixer: Converted {converted} materials to URP/Lit. Skipped {skipped} already-converted.");
    }
}