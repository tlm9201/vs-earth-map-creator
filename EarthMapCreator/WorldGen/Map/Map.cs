using System;
using System.IO;
using System.Linq;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace EarthMapCreator;

public class MapLayers {
    public DataMap HeightMap { get; private set; }
    public DataMap ClimateMap { get; private set; }
    public DataMap TreeMap { get; private set; }
    public DataMap RiverMap { get; private set; }

    public MapLayers(string directory) {
        var files = Directory.GetFiles(directory);
        
        foreach (var file in files)
        {
            // log
            Console.WriteLine(file);
        }
        
        var heightmapFile = files.First(n => Path.GetFileName(n) == "heightmap.bmp");
        var landcoverFile = files.First(n => Path.GetFileName(n) == "landmask.bmp");
        var climateFile = files.First(n => Path.GetFileName(n) == "climate.bmp");
        var treeFile = files.First(n => Path.GetFileName(n) == "tree.bmp");
        var riverFile = files.First(n => Path.GetFileName(n) == "river.bmp");
        
        RiverMap = new RiverMap(riverFile);
        HeightMap = new HeightMap(heightmapFile, landcoverFile, (RiverMap)RiverMap);
        ClimateMap = new ClimateMap(climateFile);
        TreeMap = new TreeMap(treeFile);
    }
}

public abstract class DataMap
{
    // indexed by region coordinates
    public IntDataMap2D[][] IntValues { get; protected set; }
    public SKBitmap Bitmap { get; private set; }

    public DataMap(string filePath)
    {
        Bitmap = LoadBitmap(filePath);
    }

    protected static SKBitmap LoadBitmap(string filePath)
    {
        var config = EarthMapCreator.config;
        var original = SKBitmap.Decode(filePath);

        var width = original.Width;
        var height = original.Height;

        var newWidth = config.MapWidthBlocks;
        var newHeight = config.MapHeightBlocks;

        int aspectRatio = width / height;
        int configuredAspectRatio = newWidth / newHeight;
        if (aspectRatio != configuredAspectRatio) {
            // we cant scale this properly
            throw new InvalidOperationException("Aspect ratio of configured dimensions do not match the bitmap");
        }

        var newImageInfo = original.Info.WithSize(newWidth, newHeight);
        var resized = original.Resize(newImageInfo, SKFilterQuality.High);
        original.Dispose();
        
        // internally we modify the IntMap of entire regions (16x16 [32x] chunks, or 512x512 blocks) 
        if (resized.Width % 512 != 0 || resized.Height % 512 != 0) {
            throw new InvalidOperationException("Width or height does not align to a region (val % 512 != 0)");
        }
        
        return resized;
    }
}