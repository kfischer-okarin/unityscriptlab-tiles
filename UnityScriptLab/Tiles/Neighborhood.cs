using System;

using UnityEngine;
using UnityEngine.Tilemaps;

namespace UnityScriptLab.Tiles {
  using D = Direction;
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

    public bool Has(Direction direction) {
      switch (direction) {
        case D.DownLeft:
          return CheckFlag(0b_100_00_000);
        case D.Down:
          return CheckFlag(0b_010_00_000);
        case D.DownRight:
          return CheckFlag(0b_001_00_000);
        case D.Left:
          return CheckFlag(0b_000_10_000);
        case D.Right:
          return CheckFlag(0b_000_01_000);
        case D.UpLeft:
          return CheckFlag(0b_000_00_100);
        case D.Up:
          return CheckFlag(0b_000_00_010);
        case D.UpRight:
          return CheckFlag(0b_000_00_001);
        default:
          return false;
      }
    }

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
