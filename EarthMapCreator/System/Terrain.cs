using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace EarthMapCreator;

public class Terrain : ModSystem
{
    private ICoreServerAPI _api;
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        _api = api;
        api.Event.ChunkColumnGeneration(Event_OnChunkColumnGeneration, EnumWorldGenPass.Terrain, "standard");
    }

    private void Event_OnChunkColumnGeneration(IChunkColumnGenerateRequest request)
    {
        int halfChunkSize = _api.WorldManager.ChunkSize / 2;
        
        int chunkX = request.ChunkX;
        int chunkZ = request.ChunkZ;
        
        int regionX = chunkX / halfChunkSize;
        int regionZ = chunkZ / halfChunkSize;
        
        if (!EarthWorldGenerator.GeneratingRegions.Exists(it => it.X == regionX && it.Y == regionZ))
        {
            //return;
        }

        GenerateTerrain(request, regionX, regionZ);
    }

    protected void GenerateTerrain(IChunkColumnGenerateRequest request, int regionX, int regionZ)
    {
        int chunkSize = _api.WorldManager.ChunkSize;
        int chunkX = request.ChunkX;
        int chunkZ = request.ChunkZ;
        
        IntDataMap2D heightMap = EarthMapCreator.Layers.HeightMap.IntValues[regionX][regionZ];
        IntDataMap2D bathyMap = EarthMapCreator.Layers.BathymetryMap.IntValues[regionX][regionZ];
        IntDataMap2D riverMap = EarthMapCreator.Layers.RiverMap.IntValues[regionX][regionZ];
        
        IServerChunk[] chunks = request.Chunks;
        
        int[,] bisectedHeightMap = CutHeightMapForChunk(heightMap, new Vec2i(chunkX, chunkZ), new Vec2i(regionX, regionZ));
        int[,] bisectedBathyMap = CutHeightMapForChunk(bathyMap, new Vec2i(chunkX, chunkZ), new Vec2i(regionX, regionZ));
        
        var minY = int.MaxValue;
        var maxY = int.MinValue;

        foreach (var height in bisectedHeightMap)
        {
            if (height > maxY)
            {
                maxY = height;
            }
            
            if (height < minY)
            {
                minY = height;
            }
        }

        int mapSizeY = _api.WorldManager.MapSizeY;
        
        // idx -> y coordinate
        bool[] layerFullySolid = new bool[mapSizeY];
        bool[] layerFullyEmpty = new bool[mapSizeY];
        
        for (int y = 0; y < mapSizeY; y++)
        {
            if (y <= minY)
            {
                layerFullySolid[y] = true;
            }
            else
            {
                layerFullySolid[y] = false;
            }
            
            if (y > maxY)
            {
                layerFullyEmpty[y] = true;
            }
            else
            {
                layerFullyEmpty[y] = false;
            }
        }
        
        var chunk = chunks[0];
        var chunkData = chunk.Data;

        ushort[] rainHeightMap = chunk.MapChunk.RainHeightMap;
        ushort[] terrainHeightMap = chunk.MapChunk.WorldGenTerrainHeightMap;
        
        // 0 is bedrock
        var config = GlobalConfig.GetInstance(_api);
        int bedrock = config.mantleBlockId;
        int rock = config.defaultRockId;
        int seaLevel = TerraGenConfig.seaLevel;
        
        chunkData.SetBlockBulk(0, chunkSize, chunkSize, bedrock);
        
        int yTop = mapSizeY - 2;
        int yBase = 1;
        for (; yBase < yTop - 1; yBase++)
        {
            if (layerFullySolid[yBase])
            {
                // rock
                if (yBase % chunkSize == 0)
                {
                    chunkData = chunks[yBase / chunkSize].Data;
                }
                
                chunkData.SetBlockBulk((yBase % chunkSize) * chunkSize * chunkSize, chunkSize, chunkSize, rock);
            }
            // otherwise, mixed layer (surface)
            else
            {
                break;
            }
        }

        // come from top -> down
        while (yTop >= yBase && layerFullyEmpty[yTop])
        {
            yTop--;
        }
        
        // clamp to sea level
        if (yTop < seaLevel)
        {
            yTop = seaLevel;
        }
        
        // fill mixed layer column by column
        for (int lx = 0; lx < chunkSize; lx++)
        {
            for (int lz = 0; lz < chunkSize; lz++)
            {
                int rx = lx + chunkSize * chunkX - _api.WorldManager.RegionSize * regionX;
                int rz = lz + chunkSize * chunkZ - _api.WorldManager.RegionSize * regionZ;
                
                int mapIdx = ChunkIndex2d(lx, lz);
                
                // y is the top of this column
                // ybase is the start
                // fill from ybase to y
                bool freshWater = IsFreshWaterHere(config, riverMap, rx, rz);

                int y = Math.Min(bisectedHeightMap[lx, lz], _api.WorldManager.MapSizeY - 2);
                int waterBase = y;
                if (y < seaLevel && !freshWater) y = seaLevel - 1;
                
                terrainHeightMap[mapIdx] = (ushort)(yBase - 1);
                rainHeightMap[mapIdx] = (ushort)(yBase - 1);

                // if this is fresh water, then we invert based off
                // the delta on the bathymetry map
                // and set this value to the top of the column
                if (freshWater)
                {
                    y = Math.Min(y + bisectedBathyMap[lx, lz] - 1, _api.WorldManager.MapSizeY - 2);
                }

                int block;
                
                for (int yy = yBase; yy <= y; yy++)
                {
                    int ly = yy % chunkSize;
                    int chunkIdx = ChunkIndex3d(lx, ly, lz);
                    chunkData = chunks[yy / chunkSize].Data;
                    
                    if ((freshWater || (yy < seaLevel && y < seaLevel)) && yy > waterBase)
                    {
                        if (yy == seaLevel - 1)
                        {
                            block = freshWater ? config.waterBlockId : config.saltWaterBlockId; 
                            
                            // surface
                            rainHeightMap[mapIdx] = (ushort)yy;
                        }
                        else
                        {
                            block = config.waterBlockId;
                        }
                        
                        chunkData.SetFluid(chunkIdx, block);
                    }
                    else
                    {
                        block = rock;
                        chunkData[chunkIdx] = block;
                        terrainHeightMap[mapIdx] = (ushort)yy;
                        rainHeightMap[mapIdx] = (ushort)yy;
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ChunkIndex3d(int x, int y, int z)
    {
        int chunkSize = _api.WorldManager.ChunkSize;
        return (y * chunkSize + z) * chunkSize + x;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ChunkIndex2d(int x, int z)
    {
        int chunksize = _api.WorldManager.ChunkSize;
        return z * chunksize + x;
    }
    
    private int[,] CutHeightMapForChunk(IntDataMap2D heightMap, Vec2i chunkCoords, Vec2i regionCoords)
    {
        int chunkSize = _api.WorldManager.ChunkSize;
        int chunkSized2 = chunkSize / 2;
        
        int[,] chunkElevation = new int[chunkSize, chunkSize];

        // top left is least most 
        // e.g. region x/z              1000, 1000
        // topleft chunk x/z            16000,16000 
        // bottom right chunk x/z       16015,16015
        // global top left chunk coordinate of this region
        Vec2i regionTopLeftChunk = new Vec2i(
            chunkSized2 * regionCoords.X,
            chunkSized2 * regionCoords.Y
        );
        
        // subtract 
        Vec2i localChunk = chunkCoords - regionTopLeftChunk;
        
        int maxX = (1+localChunk.X) * chunkSize;
        int maxZ = (1+localChunk.Y) * chunkSize;

        int minX = localChunk.X * chunkSize;
        int minZ = localChunk.Y * chunkSize;
        
        int lx = 0;
        int lz = 0;
        for (int x = minX; x < maxX; x++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                int height = heightMap.GetInt(x, z);
                chunkElevation[lx, lz] = height;
                lz++;
            }

            lx++;
            lz = 0;
        }
        
        return chunkElevation;
    }

    private bool IsFreshWaterHere(GlobalConfig config, IntDataMap2D riverMap, int x, int z)
    {
        return riverMap.GetInt(x, z) > 0;
    }
}