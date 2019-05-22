using UnityEditor;

using UnityEngine;
using UnityEngine.Tilemaps;

namespace UnityScriptLab.Tiles {

  [CustomEditor(typeof(TerrainAutotile), true)]
  class TerrainAutotileEditor : Editor {
    private TerrainAutotile tile { get { return (target as TerrainAutotile); } }

    public override void OnInspectorGUI() {
      EditorGUI.BeginChangeCheck();
      Texture2D oldBaseTexture = tile.baseTexture;

      tile.colliderType = (Tile.ColliderType) EditorGUILayout.EnumFlagsField("Collider Type", tile.colliderType);
      tile.baseTexture = (Texture2D) EditorGUILayout.ObjectField("Base Texture", tile.baseTexture, typeof(Texture2D), false);
      if (tile.baseTexture != oldBaseTexture) {
        UpdateNestedAssets();
      }
      if (EditorGUI.EndChangeCheck())
        EditorUtility.SetDirty(tile);
    }

    void UpdateNestedAssets() {
      foreach (Sprite s in tile.Sprites) {
        AssetDatabase.RemoveObjectFromAsset(s.texture);
        AssetDatabase.RemoveObjectFromAsset(s);
      }
      tile.CalcSprites();

      foreach (Sprite s in tile.Sprites) {
        s.texture.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(s.texture, tile);
        s.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(s, tile);
      }
      AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(tile));
    }
  }
}
