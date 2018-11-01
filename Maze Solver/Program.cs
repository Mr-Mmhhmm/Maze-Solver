using Maze_Solver.Classes;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Maze_Solver
{
    /// <summary>
    /// Rules: 
    /// -Should have a starting point in the top row.
    /// -Should have an exit in the bottom row.
    /// </summary>
    class Program
    {

        [STAThread]
        static void Main(string[] args)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK && File.Exists(dialog.FileName))
            {
                Bitmap inputImage = Image.FromFile(dialog.FileName) as Bitmap;
                Map map = Map.BuildMap(inputImage);

                Point[][] paths = map.CollectCoins();

                Image solution = inputImage.Clone() as Image;

                if (paths != null)
                {
                    Graphics graphics = Graphics.FromImage(solution);
                    foreach (Point[] path in paths) graphics.DrawLines(Pens.Red, path);
                    solution.Save(dialog.FileName.Remove(dialog.FileName.Length - 4) + " Solved.bmp");
                }
                else solution.Save(dialog.FileName.Remove(dialog.FileName.Length - 4) + " Unsolveable.bmp");
            }
        }
    }
}
