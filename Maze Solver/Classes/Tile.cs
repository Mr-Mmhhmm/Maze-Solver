
using System;
using System.Drawing;

namespace Maze_Solver.Classes
{
    public enum TileType { Empty, Wall, Coin }

    public class Tile
    {
        public Point Location;
        public bool Walkable { get { return type != TileType.Wall; } }
        public TileType type;

        public Tile(Point point, TileType tileType)
        {
            Location = point;
            type = tileType;
        }
    }
}
