using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class GameTile : Tile
{
    #if UNITY_EDITOR
    // The following is a helper that adds a menu item to create a RoadTile Asset
    [MenuItem("Assets/Create/GameTile")]
    public static void CreateGameTile()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Game Tile", "New Game Tile", "Asset", "Save Game Tile", "Assets");
      
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameTile>(), path);
    }
    #endif
}