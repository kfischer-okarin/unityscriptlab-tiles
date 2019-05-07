using NSubstitute;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.Tilemaps;

using UnityScriptLab.Tiles;

namespace Tests.Tiles {
  public class NeighborhoodTests {
    TileBase tile = ScriptableObject.CreateInstance<Tile>();
    ITilemap tilemap;

    [Test]
    public void UpdateTest() {
      tilemap = Substitute.For<ITilemap>();

      tilemap.GetTile(Vector3Int.up).Returns(tile);
      tilemap.GetTile(Vector3Int.down).Returns(tile);
      Neighborhood n = new Neighborhood(tile, tilemap, Vector3Int.zero);

      Assert.That(n.HasTop, Is.True);
      Assert.That(n.HasBottom, Is.True);
      Assert.That(n.HasRight, Is.False);
    }
  }
}
