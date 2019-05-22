using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;

using UnityScriptLab.Tiles.Util;

namespace UnityScriptLab.Tiles {
  using D = Direction;

  [Serializable]
  [CreateAssetMenu(fileName = "New Terrain Autotile", menuName = "Tiles/Terrain Autotile")]
  public class TerrainAutotile : TileBase {
    public Tile.ColliderType colliderType = Tile.ColliderType.Sprite;
    public Texture2D baseTexture;

    [SerializeField]
    Dictionary<TileParts, Sprite> sprites = new Dictionary<TileParts, Sprite>();

    public override void RefreshTile(Vector3Int location, ITilemap tileMap) {
      for (int y = -1; y <= 1; y++)
        for (int x = -1; x <= 1; x++) {
          Vector3Int position = new Vector3Int(location.x + x, location.y + y, location.z);
          TileBase t = tileMap.GetTile(position);
          if (t == this) {
            tileMap.RefreshTile(position);
          }
        }
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData) {
      tileData.colliderType = colliderType;
      tileData.color = Color.white;
      tileData.transform = Matrix4x4.identity;
      Neighborhood neighborhood = new Neighborhood(this, tilemap, position);
      TileParts tileParts = TileParts.Construct(neighborhood);

      if (!sprites.ContainsKey(tileParts)) {
        sprites[tileParts] = BuildSprite(tileParts);
      }
      tileData.sprite = sprites[tileParts];
    }

    void OnValidate() {
      Assert.IsTrue(baseTexture.isReadable, "baseTexture must be readable");
    }

    [Serializable]
    public struct TileParts {
      public(int, int) TopLeft => (partCoordinates[0], partCoordinates[1]);
      public(int, int) TopRight => (partCoordinates[2], partCoordinates[3]);
      public(int, int) BottomLeft => (partCoordinates[4], partCoordinates[5]);
      public(int, int) BottomRight => (partCoordinates[6], partCoordinates[7]);

      [SerializeField]
      List<int> partCoordinates;

      public static TileParts Construct(Neighborhood neighborhood) {
        return new TileParts(
          CalcTopLeft(neighborhood).Position,
          CalcTopLeft(neighborhood, flipHorizontal : true).Position,
          CalcTopLeft(neighborhood, flipVertical : true).Position,
          CalcTopLeft(neighborhood, flipHorizontal : true, flipVertical : true).Position
        );
      }

      public override string ToString() {
        return $"TileParts({TopLeft}, {TopRight}, {BottomLeft}, {BottomRight})";
      }

      TileParts((int, int) topLeft, (int, int) topRight, (int, int) bottomLeft, (int, int) bottomRight) {
        this.partCoordinates = new List<int> {
          topLeft.Item1,
          topLeft.Item2,
          topRight.Item1,
          topRight.Item2,
          bottomLeft.Item1,
          bottomLeft.Item2,
          bottomRight.Item1,
          bottomRight.Item2
        };
      }

      public override bool Equals(object obj) {
        if (!(obj is TileParts)) {
          return false;
        }

        TileParts self = this;
        TileParts other = (TileParts) obj;

        return Enumerable.Range(0, 8).All(i => self.partCoordinates[i] == other.partCoordinates[i]);
      }

      public override int GetHashCode() {
        string stringToHash = String.Join(",", partCoordinates.ConvertAll(i => i.ToString()));
        return stringToHash.GetHashCode();
      }

      static TilePart CalcTopLeft(Neighborhood neighborhood, bool flipHorizontal = false, bool flipVertical = false) {
        Func<D, D> applyFlip = d => d.Flip(flipHorizontal, flipVertical);
        D up = applyFlip(D.Up);
        D left = applyFlip(D.Left);
        D upLeft = applyFlip(D.UpLeft);
        D downRight = applyFlip(D.DownRight);
        D down = applyFlip(D.Down);
        D right = applyFlip(D.Right);

        if (neighborhood.Has(up) && neighborhood.Has(left)) {
          if (neighborhood.Has(upLeft)) {
            return new AreaCenter(downRight);
          } else {
            return new ConvexCorner(upLeft);
          }
        }
        if (neighborhood.Has(up) && !neighborhood.Has(left)) {
          return new Edge(left, down);
        }
        if (!neighborhood.Has(up) && neighborhood.Has(left)) {
          return new Edge(up, right);
        }
        if (!neighborhood.Has(up) && !neighborhood.Has(left) && neighborhood.Has(right) && neighborhood.Has(down)) {
          return new Corner(upLeft);
        }
        return new SingleTileCorner(upLeft);
      }
    }

    Sprite BuildSprite(TileParts tileParts) {
      if (!baseTexture.isReadable) {
        return null;
      }
      (int width, int height) = PartDimensions;
      Texture2D target = new Texture2D(width * 2, height * 2, baseTexture.format, false);
      target.filterMode = baseTexture.filterMode;
      target.anisoLevel = baseTexture.anisoLevel;

      target.SetPixels(0, 0, width, height, TilePartPixels(tileParts.BottomLeft));
      target.SetPixels(width, 0, width, height, TilePartPixels(tileParts.BottomRight));
      target.SetPixels(0, height, width, height, TilePartPixels(tileParts.TopLeft));
      target.SetPixels(width, height, width, height, TilePartPixels(tileParts.TopRight));
      target.Apply();

      return Sprite.Create(target, new Rect(0, 0, width * 2, height * 2), new Vector2(0.5f, 0.5f), width * 2);
    }

    (int, int) PartDimensions => (baseTexture.width / 4, baseTexture.height / 6);

    Color[] TilePartPixels((int, int) position) {
      (int width, int height) = PartDimensions;
      return baseTexture.GetPixels(width * position.Item1, height * position.Item2, width, height);
    }

    interface TilePart {
      TilePart Flip(bool horizontal = false, bool vertical = false);
      (int, int) Position { get; }
    }

    class Corner : TilePart {
      protected Direction direction;

      public Corner(Direction direction) {
        Assert.IsTrue(direction.IsCorner());
        this.direction = direction;
      }

      public TilePart Flip(bool horizontal = false, bool vertical = false) => new Corner(direction.Flip(horizontal, vertical));

      public virtual(int, int) Position => (direction.Contains(D.Left) ? 0 : 3, direction.Contains(D.Up) ? 3 : 0);
    }

    class ConvexCorner : Corner {
      public ConvexCorner(Direction direction) : base(direction) { }

      public override(int, int) Position => (direction.Contains(D.Left) ? 2 : 3, direction.Contains(D.Up) ? 5 : 4);
    }

    class AreaCenter : Corner {
      public AreaCenter(Direction direction) : base(direction) { }

      public override(int, int) Position => (direction.Contains(D.Left) ? 1 : 2, direction.Contains(D.Up) ? 2 : 1);
    }

    class SingleTileCorner : Corner {
      public SingleTileCorner(Direction direction) : base(direction) { }

      public override(int, int) Position => (direction.Contains(D.Left) ? 0 : 1, direction.Contains(D.Up) ? 5 : 4);
    }

    class Edge : TilePart {
      Direction main;
      Direction secondary;

      public Edge(Direction main, Direction secondary) {
        Assert.IsFalse(main.IsCorner());
        Assert.IsFalse(secondary.IsCorner());
        Assert.IsFalse(main.IsOnSameAxis(secondary));
        this.main = main;
        this.secondary = secondary;
      }

      public TilePart Flip(bool horizontal = false, bool vertical = false) {
        return new Edge(main.Flip(horizontal, vertical), secondary.Flip(horizontal, vertical));
      }

      public(int, int) Position {
        get {
          if (main == D.Up || main == D.Down) {
            return (secondary == D.Left ? 1 : 2, main == D.Up ? 3 : 0);
          } else {
            return (main == D.Left ? 0 : 3, secondary == D.Up ? 2 : 1);
          }
        }
      }
    }
  }
}
