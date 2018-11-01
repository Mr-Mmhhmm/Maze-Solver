using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maze_Solver.Classes
{

    public class Map
    {
        private int start = -1;
        private int finish = -1;

        public Size Size { get; }
        public int Width { get { return Size.Width; } }
        public int Height { get { return Size.Height; } }
        private List<Tile> coins = new List<Tile>();
        private Tile[] tiles;

        public Map(Size size)
        {
            Size = size;
            tiles = new Tile[size.Width * size.Height];
        }

        public Tile this[int x, int y]
        {
            get => tiles[(y * Height) + x];
            set
            {
                int index = (y * Height) + x;
                if (tiles[index] != null && tiles[index].type == TileType.Coin) coins.Remove(tiles[index]);
                if (value.type == TileType.Coin) coins.Add(value);
                tiles[index] = value;
            }
        }

        private void DetermineStartAndEnd()
        {
            for (int x = 1; x < Width - 1 && (start == -1 || finish == -1); x++)
            {
                if (start == -1 && this[x, 0].type == TileType.Empty) start = x;
                if (finish == -1 && this[x, Height - 1].type == TileType.Empty) finish = x;
            }
        }

        public static Map BuildMap(Bitmap image)
        {
            Map map = new Map(image.Size);

            BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);

            unsafe
            {
                byte* bytes = (byte*)bitmapData.Scan0;
                byte bytesPerPixel = (byte)(bitmapData.Stride / bitmapData.Width);
                for (int y = 0; y < image.Height; y++)
                {
                    int row = y * bitmapData.Stride;
                    for (int x = 0; x < bitmapData.Width; x++)
                    {
                        int index = row + (x * bytesPerPixel);
                        int value = bytes[index] << 16 | bytes[index + 1] << 8 | bytes[index + 2];

                        TileType tileType;
                        if (value == 0xffffff) tileType = TileType.Empty;
                        else if (value == 0x18caff) tileType = TileType.Coin;
                        else tileType = TileType.Wall;

                        map[x, y] = new Tile(new Point(x, y), tileType);
                    }
                }
            }

            image.UnlockBits(bitmapData);

            map.DetermineStartAndEnd();

            return map;
        }

        public Point[][] CollectCoins()
        {
            if (start == -1 || finish == -1) return null;
            Point startPoint = new Point(start, 0);
            Point finishPoint = new Point(finish, Height - 1);
            List<Point[]> paths = new List<Point[]>();

            foreach(Tile coin in coins)
            {
                Point[] pathToCoin = GetPath(startPoint, coin.Location);
                if (pathToCoin != null)
                {
                    paths.Add(pathToCoin);
                    startPoint = coin.Location;
                }
                Untrample();
            }

            Point[] pathToFinish = GetPath(startPoint, finishPoint);

            if (pathToFinish != null)
            {
                paths.Add(pathToFinish);
                return paths.ToArray();
            }
            else return null;
        }

        public Point[] GetPath(Point start, Point finish)
        {

            List<Point> path = new List<Point>();
            if (!Search(start)) return null;
            else
            {
                path.Add(start);
                path.Reverse();
                return path.ToArray();
            }



            bool Search(Point currentPosition)
            {
                List<Tile> possibleMovements = new List<Tile>();

                for (int y = -1; y <= 1; y += 2)
                {
                    int newY = currentPosition.Y + y;
                    if (newY > -1 && newY < Height) TryTile(currentPosition.X, newY);
                }

                for (int x = -1; x <= 1; x += 2)
                {
                    int newX = currentPosition.X + x;
                    if (newX > -1 && newX < Width) TryTile(newX, currentPosition.Y);
                }

                possibleMovements.Sort((x, y) => x.Cost.CompareTo(y.Cost)); //TODO: sort by overall path cost.

                foreach (Tile tile in possibleMovements)
                {
                    if (tile.Location == finish)
                    {
                        path.Add(tile.Location);
                        return true;
                    }
                    else
                    {
                        if (Search(tile.Location))
                        {
                            path.Add(tile.Location);
                            return true;
                        }
                    }
                }
                return false;



                void TryTile(int x, int y)
                {
                    Tile tile = this[x, y];
                    if (tile.Walkable && !tile.tried)
                    {
                        tile.tried = true;
                        possibleMovements.Add(tile);
                    }
                }
            }
        }

        public void Untrample()
        {
            foreach (Tile tile in tiles) tile.tried = false;
        }
    }
}
