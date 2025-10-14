using System;
using SkiaSharp;
using Vintagestory.API.Datastructures;
using Vintagestory.ServerMods;

namespace EarthMapCreator;

public class HeightMap : DataMap
{
    // 108
    const int SeaFloor = 90;
    const int SeaLevel = 110;

    // sea level: 110
    public HeightMap(string filePath, string landcoverFile, RiverMap rivers) : base(filePath)
    {
        SKBitmap landcoverBmp = LoadBitmap(landcoverFile);
        
        var watch = System.Diagnostics.Stopwatch.StartNew();
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
                        SKColor lcPixel = landcoverBmp.GetPixel(posX, posZ);
                        SKColor heightPixel = Bitmap.GetPixel(posX, posZ);
                        lcPixel.ToHsv(out _, out _, out float lcBrightness);

                        bool isLand = lcBrightness >= 50.0;
                        int height = SeaFloor;

                        if (isLand)
                        {
                            height = SeaLevel + heightPixel.Red;
                        }

                        // rivers/lakes
                        int riverHere = rivers.IntValues[x][z].GetInt(i, j);
                        if (riverHere > 0 && isLand)
                        {
                            int diffFromMin = riverHere - rivers.Min;
                            int maxDiff = 255 - rivers.Min;
                            
                            float riverNormalized = (float) diffFromMin / maxDiff;
                            int riverDepth = (int) (EarthMapCreator.config.RiverDepth * riverNormalized) + 1;
                            height -= riverDepth;
                        }

                        IntValues[x][z].SetInt(i, j, height);
                    }
                }
            }
        }

        landcoverBmp.Dispose();
        Bitmap.Dispose();
        watch.Stop();
        
        Console.WriteLine("Created heightmap in {0}ms", watch.ElapsedMilliseconds);
    }

}