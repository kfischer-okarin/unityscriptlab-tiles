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
      TileParts tileParts = GetTileParts(neighborhood);
      tileData.sprite = BuildSprite(tileParts);
    }

    class Corner {
      protected Direction direction;

      public Corner(Direction direction) {
        Assert.IsTrue(direction.IsCorner());
        this.direction = direction;
      }

      public Corner FlipHorizontal => new Corner(direction.FlipHorizontal());

      public Corner FlipVertical => new Corner(direction.FlipVertical());

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

    class SingleWidthCorner : Corner {
      public SingleWidthCorner(Direction direction) : base(direction) { }

      public override(int, int) Position => (direction.Contains(D.Left) ? 0 : 1, direction.Contains(D.Up) ? 5 : 4);
    }

    class Edge {
      Direction main;
      Direction secondary;

      public Edge(Direction main, Direction secondary) {
        Assert.IsFalse(main.IsCorner());
        Assert.IsFalse(secondary.IsCorner());
        Assert.IsFalse(main.SameAxis(secondary));
        this.main = main;
        this.secondary = secondary;
      }

      public Edge FlipHorizontal => new Edge(main.FlipHorizontal(), secondary.FlipHorizontal());

      public Edge FlipVertical => new Edge(main.FlipVertical(), secondary.FlipVertical());

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

    // From bottom left to top right
    enum TilePart {
      BLL,
      BL,
      BR,
      BRR,
      LB,
      CBL,
      CBR,
      RB,
      LT,
      CTL,
      CTR,
      RT,
      TLL,
      TL,
      TR,
      TRR,
      Single_BL,
      Single_BR,
      Corner_BL,
      Corner_BR,
      Single_TL,
      Single_TR,
      Corner_TL,
      Corner_TR
    }

    TilePart TopLeft(Neighborhood neighborhood) {
      if (neighborhood.HasTop() && neighborhood.HasLeft()) {
        return neighborhood.HasTopLeft() ? TilePart.CBR : TilePart.Corner_TL;
      }
      if (neighborhood.HasTop() && !neighborhood.HasLeft()) {
        return TilePart.LB;
      }
      if (!neighborhood.HasTop() && neighborhood.HasLeft()) {
        return TilePart.TR;
      }
      if (!neighborhood.HasTop() && !neighborhood.HasLeft() && neighborhood.HasRight() && neighborhood.HasBottom()) {
        return TilePart.TLL;
      }
      return TilePart.Single_TL;
    }

    TilePart TopRight(Neighborhood neighborhood) {
      if (neighborhood.HasTop() && neighborhood.HasRight()) {
        return neighborhood.HasTopRight() ? TilePart.CBL : TilePart.Corner_TR;
      }
      if (neighborhood.HasTop() && !neighborhood.HasRight()) {
        return TilePart.RB;
      }
      if (!neighborhood.HasTop() && neighborhood.HasRight()) {
        return TilePart.TL;
      }
      if (!neighborhood.HasTop() && !neighborhood.HasRight() && neighborhood.HasLeft() && neighborhood.HasBottom()) {
        return TilePart.TRR;
      }
      return TilePart.Single_TR;
    }

    TilePart BottomLeft(Neighborhood neighborhood) {
      if (neighborhood.HasBottom() && neighborhood.HasLeft()) {
        return neighborhood.HasBottomLeft() ? TilePart.CTR : TilePart.Corner_BL;
      }
      if (neighborhood.HasBottom() && !neighborhood.HasLeft()) {
        return TilePart.LT;
      }
      if (!neighborhood.HasBottom() && neighborhood.HasLeft()) {
        return TilePart.BR;
      }
      if (!neighborhood.HasBottom() && !neighborhood.HasLeft() && neighborhood.HasRight() && neighborhood.HasBottom()) {
        return TilePart.BLL;
      }
      return TilePart.Single_BL;
    }

    TilePart BottomRight(Neighborhood neighborhood) {
      if (neighborhood.HasBottom() && neighborhood.HasRight()) {
        return neighborhood.HasBottomRight() ? TilePart.CTL : TilePart.Corner_BR;
      }
      if (neighborhood.HasBottom() && !neighborhood.HasRight()) {
        return TilePart.RT;
      }
      if (!neighborhood.HasBottom() && neighborhood.HasRight()) {
        return TilePart.BL;
      }
      if (!neighborhood.HasBottom() && !neighborhood.HasRight() && neighborhood.HasLeft() && neighborhood.HasBottom()) {
        return TilePart.BRR;
      }
      return TilePart.Single_BR;
    }
    struct TileParts {
      public TilePart topLeft;
      public TilePart topRight;
      public TilePart bottomLeft;
      public TilePart bottomRight;

      public TileParts(TilePart bottomLeft, TilePart bottomRight, TilePart topLeft, TilePart topRight) {
        this.topLeft = topLeft;
        this.topRight = topRight;
        this.bottomLeft = bottomLeft;
        this.bottomRight = bottomRight;
      }

      public override string ToString() {
        return $"TileParts({topLeft}, {topRight}, {bottomLeft}, {bottomRight})";
      }
    }

    TileParts GetTileParts(Neighborhood neighborhood) {
      return new TileParts(BottomLeft(neighborhood), BottomRight(neighborhood), TopLeft(neighborhood), TopRight(neighborhood));
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
      return (width * ((int) part % 4), height * ((int) part / 4));
    }
  }
}
