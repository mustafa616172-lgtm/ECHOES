using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script to setup Cursed Priest character materials with proper textures.
/// Run from Tools > Setup Cursed Priest Materials
/// </summary>
public class SetupCursedPriestMaterials : Editor
{
    private const string MATERIAL_PATH = "Assets/Cursed Priest/Mesh/Material/";
    private const string TEXTURE_PATH = "Assets/Cursed Priest/Mesh/Textures/";
    
    [MenuItem("Tools/Setup Cursed Priest Materials")]
    public static void SetupMaterials()
    {
        int successCount = 0;
        
        // Setup Body material
        if (SetupMaterial("Body", "Priest_Body"))
            successCount++;
        
        // Setup Head material
        if (SetupMaterial("Head", "Priest_Head"))
            successCount++;
        
        // Setup Upper material
        if (SetupMaterial("Upper", "Priest_Upper"))
            successCount++;
        
        // Setup Accesories material
        if (SetupMaterial("Accesories", "Priest_Accesories"))
            successCount++;
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"<color=green>[Cursed Priest Setup]</color> Successfully configured {successCount}/4 materials!");
    }
    
    private static bool SetupMaterial(string materialName, string texturePrefix)
    {
        string matPath = $"{MATERIAL_PATH}{materialName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        
        if (mat == null)
        {
            Debug.LogError($"[Cursed Priest Setup] Could not find material at: {matPath}");
            return false;
        }
        
        // Load textures
        Texture2D albedo = LoadTexture($"{texturePrefix}_AlbedoTransparency.png");
        Texture2D normal = LoadTexture($"{texturePrefix}_Normal.png");
        Texture2D metallic = LoadTexture($"{texturePrefix}_MetallicSmoothness.png");
        Texture2D ao = LoadTexture($"{materialName}_AO.png");
        
        // Assign textures to URP Lit shader properties
        if (albedo != null)
        {
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTexture("_MainTex", albedo); // Fallback property
            Debug.Log($"  - Assigned Albedo: {albedo.name}");
        }
        
        if (normal != null)
        {
            // Ensure normal map is properly configured
            string normalPath = AssetDatabase.GetAssetPath(normal);
            TextureImporter importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
            Debug.Log($"  - Assigned Normal: {normal.name}");
        }
        
        if (metallic != null)
        {
            mat.SetTexture("_MetallicGlossMap", metallic);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Smoothness", 0.5f);
            Debug.Log($"  - Assigned Metallic: {metallic.name}");
        }
        
        if (ao != null)
        {
            mat.SetTexture("_OcclusionMap", ao);
            mat.EnableKeyword("_OCCLUSIONMAP");
            Debug.Log($"  - Assigned AO: {ao.name}");
        }
        
        EditorUtility.SetDirty(mat);
        Debug.Log($"<color=cyan>[Cursed Priest Setup]</color> Configured material: {materialName}");
        
        return true;
    }
    
    private static Texture2D LoadTexture(string fileName)
    {
        string path = $"{TEXTURE_PATH}{fileName}";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        
        if (tex == null)
        {
            Debug.LogWarning($"[Cursed Priest Setup] Could not find texture: {path}");
        }
        
        return tex;
    }
}
