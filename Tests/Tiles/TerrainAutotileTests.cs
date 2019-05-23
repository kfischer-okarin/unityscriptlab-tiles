using NSubstitute;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.Tilemaps;

using UnityScriptLab.Tiles;

namespace Tests.Tiles {
  public class TerrainAutotilesTest {
    ITilemap tilemap;
    TerrainAutotile tile;
    TileData result;

    [SetUp]
    public void Prepare() {
      tilemap = Substitute.For<ITilemap>();
      tile = ScriptableObject.CreateInstance<TerrainAutotile>();
      tile.baseTexture = BuildFakeTexture();
      result = new TileData();
      tile.CalcSprites();
    }

    Color[,] texture = new Color[4, 6];

    Texture2D BuildFakeTexture() {
      Texture2D result = new Texture2D(4, 6);
      for (int x = 0; x < 4; x++) {
        for (int y = 0; y < 6; y++) {
          result.SetPixel(x, y, new Color(x * 0.1f, y * 0.1f, 0));
          texture[x, y] = result.GetPixel(x, y);
        }
      }
      return result;
    }

    void AssertTile(params(int, int) [] expectedPixels) {
      tile.GetTileData(Vector3Int.zero, tilemap, ref result);
      Color[] pixels = result.sprite.texture.GetPixels(0, 0, 2, 2);
      for (int i = 0; i < 4; i++) {
        (int x, int y) = expectedPixels[i];
        Assert.That(pixels[i] == texture[x, y], $"But pixel {i} was {pixels[i]} ({texture[x, y]})");
      }
    }

    [Test]
    public void NoNeighborsTest() {
      tile.GetTileData(Vector3Int.zero, tilemap, ref result);
      AssertTile((0, 4), (1, 4), (0, 5), (1, 5));
    }

    [Test]
    public void OneSideTest() {
      tilemap.GetTile(Vector3Int.up).Returns(tile);
      tile.GetTileData(Vector3Int.zero, tilemap, ref result);
      AssertTile((0, 4), (1, 4), (0, 1), (3, 1));
    }

    [Test]
    public void TwoSidesCornerTest() {
      tilemap.GetTile(Vector3Int.down).Returns(tile);
      tilemap.GetTile(Vector3Int.right).Returns(tile);
      tile.GetTileData(Vector3Int.zero, tilemap, ref result);
      AssertTile((0, 2), (3, 4), (0, 3), (1, 3));
    }

    [Test]
    public void ThreeSidesCornerTest() {
      tilemap.GetTile(Vector3Int.left).Returns(tile);
      tilemap.GetTile(Vector3Int.right).Returns(tile);
      tilemap.GetTile(Vector3Int.up).Returns(tile);
      tile.GetTileData(Vector3Int.zero, tilemap, ref result);
      AssertTile((2, 0), (1, 0), (2, 5), (3, 5));
    }
  }
}
