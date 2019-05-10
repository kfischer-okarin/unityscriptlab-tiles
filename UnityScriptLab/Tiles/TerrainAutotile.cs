using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;

namespace UnityScriptLab.Tiles {
  using D = Direction;
  [Serializable]
  [CreateAssetMenu(fileName = "New Terrain Auto Tile", menuName = "Tiles/Terrain Auto Tile")]
  public class TerrainAutotile : TileBase {
    public Tile.ColliderType colliderType = Tile.ColliderType.Sprite;
    public Texture2D baseTexture;

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
      tileData.sprite = BuildSprite(tileParts);
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

      public override bool Equals(object obj) {
        Corner c = obj as Corner;
        return c != null && direction == c.direction;
      }

      public override int GetHashCode() => $"{GetType().Name}-{direction}".GetHashCode();
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

      public (int, int) Position {
        get {
          if (main == D.Up || main == D.Down) {
            return (secondary == D.Left ? 1: 2, main == D.Up ? 3 : 0);
          } else {
            return (main == D.Left ? 0 : 3, secondary == D.Up ? 2 : 1);
          }
        }
      }

      public override bool Equals(object obj) {
        Edge e = obj as Edge;
        return e != null && main == e.main && secondary == e.secondary;
      }

      public override int GetHashCode() => $"{GetType().Name}-{main}-{secondary}".GetHashCode();
    }

    static TilePart TopLeft(Neighborhood neighborhood, bool flipHorizontal = false, bool flipVertical = false) {
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

    struct TileParts {
      public TilePart topLeft;
      public TilePart topRight;
      public TilePart bottomLeft;
      public TilePart bottomRight;

      public static TileParts Construct(Neighborhood neighborhood) {
        return new TileParts(
          TopLeft(neighborhood),
          TopLeft(neighborhood, flipHorizontal: true),
          TopLeft(neighborhood, flipVertical: true),
          TopLeft(neighborhood, flipHorizontal: true, flipVertical: true)
        );
      }

      TileParts(TilePart topLeft, TilePart topRight, TilePart bottomLeft, TilePart bottomRight) {
        this.topLeft = topLeft;
        this.topRight = topRight;
        this.bottomLeft = bottomLeft;
        this.bottomRight = bottomRight;
      }

      public override string ToString() {
        return $"TileParts({topLeft}, {topRight}, {bottomLeft}, {bottomRight})";
      }
    }

    Dictionary<TileParts, Sprite> sprites = new Dictionary<TileParts, Sprite>();

    Sprite BuildSprite(TileParts tileParts) {
      if (sprites.ContainsKey(tileParts)) {
        return sprites[tileParts];
      }
      (int width, int height) = PartDimensions;
      Texture2D target = new Texture2D(width * 2, height * 2);

      int x;
      int y;

      (x, y) = TilePartPosition(tileParts.bottomLeft);
      target.SetPixels(0, 0, width, height, baseTexture.GetPixels(x, y, width, height));
      (x, y) = TilePartPosition(tileParts.bottomRight);
      target.SetPixels(width, 0, width, height, baseTexture.GetPixels(x, y, width, height));
      (x, y) = TilePartPosition(tileParts.topLeft);
      target.SetPixels(0, height, width, height, baseTexture.GetPixels(x, y, width, height));
      (x, y) = TilePartPosition(tileParts.topRight);
      target.SetPixels(width, height, width, height, baseTexture.GetPixels(x, y, width, height));
      target.Apply();

      Sprite result = Sprite.Create(target, new Rect(0, 0, width * 2, height * 2), new Vector2(0.5f, 0.5f), width * 2);
      sprites[tileParts] = result;
      return result;
    }

    (int, int) PartDimensions => (baseTexture.width / 4, baseTexture.height / 6);

    (int, int) TilePartPosition(TilePart part) {
      (int width, int height) = PartDimensions;
      (int x, int y) = part.Position;
      return (width * x, height * y);
    }
  }
}
