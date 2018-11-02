
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

        public Point[] CollectCoins()
        {
            if (start == -1 || finish == -1) return null;
            Point startPoint = new Point(start, 0);
            Point finishPoint = new Point(finish, Height - 1);
            Point[] path = GetPath(startPoint, finishPoint, coins.Select(x => x.Location).ToList());
            return path;
        }

        public Point[] GetPath(Point start, Point finish, List<Point> midPoints)
        {
            Point checkPoint = start;
            List<Point> route = new List<Point>() { finish };
            if (!Search(finish, start, route)) return null;
            else return route.ToArray();



            bool Search(Point currentPosition, Point destination, List<Point> path, List<Point> triedTiles = null)
            {
                if (triedTiles == null) triedTiles = new List<Point>();
                List<Tile> possibleMovements = new List<Tile>();
                int[] directions = new int[] { 1, -1 };

                foreach (int y in directions)
                {
                    int newY = currentPosition.Y + y;
                    if (newY > -1 && newY < Height) TryTile(currentPosition.X, newY);
                }

                foreach (int x in directions)
                {
                    int newX = currentPosition.X + x;
                    if (newX > -1 && newX < Width) TryTile(newX, currentPosition.Y);
                }

                Point currentDestination;
                if (midPoints.Count > 0)
                {
                    midPoints.Sort((a, b) => a.SquareDistance(currentPosition).CompareTo(b.SquareDistance(currentPosition)));
                    currentDestination = midPoints[0];
                }
                else currentDestination = destination;

                possibleMovements.Sort((a, b) => a.Location.SquareDistance(currentDestination).CompareTo(b.Location.SquareDistance(currentDestination)));

                foreach (Tile tile in possibleMovements)
                {
                    path.Add(tile.Location);
                    if (tile.Location == currentDestination)
                    {
                        if (tile.Location == destination) return true;
                        else
                        {
                            // Found a coin!
                            midPoints.Remove(currentDestination);
                            checkPoint = tile.Location;
                            // Continue searching with a fresh list of tried tiles.
                            if (Search(tile.Location, destination, path, new List<Point>())) return true;
                        }
                    }
                    else if (Search(tile.Location, destination, path, triedTiles))
                    {
                        //TODO: Try to find a shorter route.
                        return true;
                    }
                    else path.Remove(tile.Location);
                }
                return false;



                void TryTile(int x, int y)
                {
                    Tile tile = this[x, y];
                    if (tile.Walkable && !triedTiles.Contains(tile.Location))
                    {
                        triedTiles.Add(tile.Location);
                        possibleMovements.Add(tile);
                    }
                }
            }
        }
    }
}
