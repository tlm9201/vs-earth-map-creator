using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vintagestory.API.Datastructures;

namespace EarthMapCreator;

public class MapLayers {
    public DataMap<Rgb48> HeightMap { get; private set; }
    public DataMap<Rgb48> BathymetryMap { get; private set; }
    public DataMap<Rgb24> ClimateMap { get; private set; }
    public DataMap<Rgb24> TreeMap { get; private set; }
    public DataMap<Rgb24> RiverMap { get; private set; }

    public MapLayers(string directory) {
        var files = Directory.GetFiles(directory);
        
        foreach (var file in files)
        {
            // log
            Console.WriteLine(file);
        }
        
        var heightmapFile = files.First(n => Path.GetFileName(n) == "heightmap.png");
        var bathymetryFile = files.First(n => Path.GetFileName(n) == "bathymetry.png");
        var landcoverFile = files.First(n => Path.GetFileName(n) == "landmask.png");
        var climateFile = files.First(n => Path.GetFileName(n) == "climate.png");
        var treeFile = files.First(n => Path.GetFileName(n) == "tree.png");
        var riverFile = files.First(n => Path.GetFileName(n) == "river.png");
        
        RiverMap = new RiverMap(riverFile);
        BathymetryMap = new BathymetryMap(bathymetryFile);
        HeightMap = new HeightMap(heightmapFile, landcoverFile, (RiverMap)RiverMap, (BathymetryMap)BathymetryMap);
        ClimateMap = new ClimateMap(climateFile);
        TreeMap = new TreeMap(treeFile);
    }
}

public abstract class DataMap<T> where T : unmanaged, IPixel<T>
{
    // indexed by region coordinates
    public IntDataMap2D[][] IntValues { get; protected set; }
    //public SKBitmap Bitmap { get; private set; }
    public Image<T> Bitmap { get; private set; }

    public DataMap(string filePath)
    {
        Bitmap = LoadBitmap<T>(filePath);
    }

    protected static Image<T> LoadBitmap<T>(string filePath) where T : unmanaged, IPixel<T>
    {
        var config = EarthMapCreator.config;
        var img = Image.Load<T>(filePath);

        var width = img.Width;
        var height = img.Height;

        var cfgWidth = config.MapWidthBlocks;
        var cfgHeight = config.MapHeightBlocks;

        if (width != cfgWidth || height != cfgHeight)
        {
            throw new InvalidOperationException($"Image dimensions do not match config: {width}x{height} != {cfgWidth}x{cfgHeight}");
        }

        // internally we modify the IntMap of entire regions (16x16 [32x] chunks, or 512x512 blocks) 
        if (img.Width % 512 != 0 || img.Height % 512 != 0) {
            throw new InvalidOperationException("Width or height does not align to a region (val % 512 != 0)");
        }

        return img;
    }
}