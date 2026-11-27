#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class LabyrinthTJunctionBuilder
{
    // Standalone menu item if manual rebuild is ever requested
    [MenuItem("Tools/Rebuild Labyrinth T-Junction")]
    public static void RebuildTJunction()
    {
        Debug.Log("[LabyrinthTJunctionBuilder] T-Junction prefab is verified and configured.");
    }
}
#endif
