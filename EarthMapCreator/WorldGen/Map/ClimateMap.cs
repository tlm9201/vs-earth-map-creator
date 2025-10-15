using SixLabors.ImageSharp.PixelFormats;
using Vintagestory.API.Datastructures;

namespace EarthMapCreator;

public class ClimateMap : DataMap<Rgb24>
{
    public ClimateMap(string filePath) : base(filePath)
    {
        // red channel: temperature
        // green channel: precipitation
        
        int xRegions = Bitmap.Width / 512;
        int zRegions = Bitmap.Height / 512;
        IntValues = new IntDataMap2D[xRegions][];
        
        for (int x = 0; x < xRegions; x++)
        {
            IntValues[x] = new IntDataMap2D[zRegions];
            for (int z = 0; z < zRegions; z++)
            {
                IntValues[x][z] = IntDataMap2D.CreateEmpty();
                IntValues[x][z].Size = 512;
                IntValues[x][z].Data = new int[512 * 512];
                
                for (int i = 0; i < 512; i++)
                {
                    for (int j = 0; j < 512; j++)
                    {
                        int posX = x * 512 + i;
                        int posZ = z * 512 + j;
                        Rgb24 pixel = Bitmap[posX, posZ];

                        byte red = pixel.R;
                        byte green = pixel.G;
                        byte blue = pixel.B;
                        int rgb = red;
                        rgb = (rgb << 8) + green;
                        rgb = (rgb << 8) + blue;

                        IntValues[x][z].SetInt(i, j, rgb);
                    }
                }
            }
        }
        
        Bitmap.Dispose();
    }
}