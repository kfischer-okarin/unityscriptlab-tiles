using System;

using UnityEngine;
using UnityEngine.Tilemaps;

namespace UnityScriptLab.Tiles {
  public class Neighborhood {
    TileBase tile;
    ITilemap tilemap;
    Vector3Int position;
    uint neighborsFlag;

    public Neighborhood(TileBase tile, ITilemap tilemap, Vector3Int position) {
      this.tile = tile;
      this.tilemap = tilemap;
      this.position = position;
      this.neighborsFlag = CalcNeighborsFlag();
    }

    public bool HasBottomLeft() => CheckFlag(0b_100_00_000);
    public bool HasBottom() => CheckFlag(0b_010_00_000);
    public bool HasBottomRight() => CheckFlag(0b_001_00_000);
    public bool HasLeft() => CheckFlag(0b_000_10_000);
    public bool HasRight() => CheckFlag(0b_000_01_000);
    public bool HasTopLeft() => CheckFlag(0b_000_00_100);
    public bool HasTop() => CheckFlag(0b_000_00_010);
    public bool HasTopRight() => CheckFlag(0b_000_00_001);

    uint CalcNeighborsFlag() {
      uint result = 0;
      uint flag = 1;
      for (int y = 1; y >= -1; y--) {
        for (int x = 1; x >= -1; x--) {
          if (x == 0 && y == 0) {
            continue;
          }

          Vector3Int neighborPos = new Vector3Int(position.x + x, position.y + y, position.z);
          if (HasTile(neighborPos)) {
            result |= flag;
          }
          flag <<= 1;
        }
      }
      return result;
    }

    bool HasTile(Vector3Int pos) {
      TileBase t = tilemap.GetTile(pos);
      return t != null && t == tile;
    }

    bool CheckFlag(uint flag) {
      return (neighborsFlag & flag) == flag;
    }

    public override string ToString() {
      return $"Neighborhood({Convert.ToString(neighborsFlag, 2)})";
    }
  }
}
