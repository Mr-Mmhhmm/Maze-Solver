
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

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

        public Tile this[Point point] => this[point.X, point.Y];

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

        public List<Point> CollectCoins()
        {
            if (start == -1 || finish == -1) return null;
            Point startPoint = new Point(start, 0);
            Point finishPoint = new Point(finish, Height - 1);
            List<Point> path = GetPath(startPoint, finishPoint, coins.Select(x => x.Location).ToList());
            return path;
        }

        public List<Point> GetPath(Point start, Point destination, List<Point> via)
        {
            Point currentPosition = destination;

            List<Point> myRoute = new List<Point>();
            List<Point> path = new List<Point>();
            List<Point> triedTiles = new List<Point>();
            List<List<Point>> possibleMovements = new List<List<Point>>();

            int[] directions = new int[] { 1, -1 };

            while (true)
            {
                List<Point> validMovements = new List<Point>();

                foreach (int y in directions)
                {
                    int newY = currentPosition.Y + y;
                    Point point = new Point(currentPosition.X, newY);
                    if (newY > -1 && newY < Height && TryTile(point))
                    {
                        triedTiles.Add(point);
                        validMovements.Add(point);
                    }
                }

                foreach (int x in directions)
                {
                    int newX = currentPosition.X + x;
                    Point point = new Point(newX, currentPosition.Y);
                    if (newX > -1 && newX < Width && TryTile(point))
                    {
                        triedTiles.Add(point);
                        validMovements.Add(point);
                    }
                }

                Point currentDestination;
                if (via.Count > 0)
                {
                    via.Sort((a, b) => a.SquareDistance(currentPosition).CompareTo(b.SquareDistance(currentPosition)));
                    currentDestination = via[0];
                }
                else currentDestination = start;

                validMovements.Sort((a, b) => a.SquareDistance(currentDestination).CompareTo(b.SquareDistance(currentDestination)));

                path.Add(currentPosition);
                possibleMovements.Add(validMovements);

                if (validMovements.Count > 0)
                {
                    currentPosition = validMovements[0];
                    validMovements.Remove(currentPosition);
                    if (currentPosition == currentDestination)
                    {
                        foreach (Point point in path) myRoute.Add(point);
                        path.Clear();

                        if (currentPosition == start)
                        {
                            myRoute.Add(start);
                            return myRoute;
                        }
                        else
                        {
                            // Found a coin!
                            via.Remove(currentDestination);
                            // Continue searching with a fresh list of tried tiles.
                            triedTiles.Clear();
                            possibleMovements.Clear();
                        }
                    }
                }
                else
                {
                    // Dead end, back track to last intersection.
                    bool invalid = true;
                    while (invalid)
                    {
                        possibleMovements.RemoveAt(possibleMovements.Count - 1);
                        path.RemoveAt(path.Count - 1);
                        if (possibleMovements.Count == 0)
                            return null;
                        else if (possibleMovements[possibleMovements.Count - 1].Count > 0)
                        {
                            currentPosition = possibleMovements[possibleMovements.Count - 1][0];
                            possibleMovements[possibleMovements.Count - 1].RemoveAt(0);
                            invalid = false;
                        }
                        else
                        {
                            invalid = true;
                        }
                    }
                }
            }

            bool TryTile(Point point)
            {
                Tile tile = this[point.X, point.Y];
                return tile.Walkable &&
                    (!triedTiles.Contains(tile.Location)
                        || (possibleMovements.Count > 0 && possibleMovements[possibleMovements.Count - 1].Contains(point)));
            }
        }
    }
}
