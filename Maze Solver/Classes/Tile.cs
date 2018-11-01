using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maze_Solver.Classes
{
    public enum TileType { Empty, Wall, Coin }

    public class Tile
    {
        public Point Location;
        public bool Walkable { get { return type != TileType.Wall; } }
        public TileType type;
        public int Cost
        {
            get
            {
                int value = int.MaxValue;
                switch (type)
                {
                    case TileType.Empty:
                        value = 0;
                        break;
                    case TileType.Wall:
                        value = int.MaxValue;
                        break;
                    case TileType.Coin:
                        value = 0;
                        break;
                    default:
                        break;
                }
                return value;
            }
        }
        public bool tried = false;

        public Tile(Point point, TileType tileType)
        {
            Location = point;
            type = tileType;
        }
    }
}
