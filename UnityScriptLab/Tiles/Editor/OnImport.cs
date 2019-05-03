#if UNITY_EDITOR && (UNITYSCRIPTLAB_TILES == false)

using UnityEditor;

using UnityEngine;

namespace UnityScriptLab.Tiles {
  [InitializeOnLoad]
  public class OnImport {
    static OnImport() {
      BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
      string definedSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
      PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, $"{definedSymbols};UNITYSCRIPTLAB_TILES");
    }
  }
}
#endif
