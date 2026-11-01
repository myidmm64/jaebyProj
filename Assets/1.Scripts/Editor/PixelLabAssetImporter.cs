using UnityEditor;
using UnityEngine;

/// <summary>Keep exported PixelLab originals on their native pixel grid.</summary>
public sealed class PixelLabAssetImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/4.UI/PixelLab/Iai/")&&!assetPath.StartsWith("Assets/4.UI/Cards/"))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=32;
        importer.filterMode=FilterMode.Point;
        importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.alphaIsTransparency=true;
        importer.wrapMode=assetPath.Contains("/Tile-")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
        if(assetPath.EndsWith("/UI-IaiPanel.png"))importer.spriteBorder=new Vector4(6,6,6,6);
    }
}
