namespace UnityScriptLab.Tiles {
  using D = Direction;

  public enum Direction {
    Up,
    UpRight,
    Right,
    DownRight,
    Down,
    DownLeft,
    Left,
    UpLeft,
  }

  static class MirrorExtensions {
    public static D FlipHorizontal(this D direction) {
      switch (direction) {
        case D.UpRight:
          return D.UpLeft;
        case D.Right:
          return D.Left;
        case D.DownRight:
          return D.DownLeft;
        case D.DownLeft:
          return D.DownRight;
        case D.Left:
          return D.Right;
        case D.UpLeft:
          return D.UpRight;
        default:
          return direction;
      }
    }

    public static D FlipVertical(this D direction) {
      switch (direction) {
        case D.Up:
          return D.Down;
        case D.UpRight:
          return D.DownRight;
        case D.DownRight:
          return D.UpRight;
        case D.Down:
          return D.Up;
        case D.DownLeft:
          return D.UpLeft;
        case D.UpLeft:
          return D.DownLeft;
        default:
          return direction;
      }
    }

    /// <summary>
    /// Is the specified direction contained in his direction?
    /// - A direction always contains itself.
    /// - UpLeft contains Up and Left.
    /// </summary>
    public static bool Contains(this D d, D direction) {
      if (d == direction) {
        return true;
      }
      switch (d) {
        case D.UpLeft:
          return direction == D.Up || direction == D.Left;
        case D.UpRight:
          return direction == D.Up || direction == D.Right;
        case D.DownRight:
          return direction == D.Down || direction == D.Right;
        case D.DownLeft:
          return direction == D.Down || direction == D.Left;
        default:
          return false;
      }
    }

    public static bool IsCorner(this D direction) {
      return direction == D.UpRight || direction == D.DownRight || direction == D.DownLeft || direction == D.UpLeft;
    }
  }
}