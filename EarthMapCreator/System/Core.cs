﻿using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace EarthMapCreator;

public class EarthMapCreator : ModSystem
{
    public static Config config;
    public static MapLayers Layers; 

    public override void StartServerSide(ICoreServerAPI api)
    {
        LoadConfig(api);
        LoadMapLayers(api);
    }

    private void LoadConfig(ICoreAPI api) 
    {
        try 
        {
            config = api.LoadModConfig<Config>("EarthMapCreator.json");
            if (config == null)
            {
                config = new Config();
            }
            api.StoreModConfig<Config>(config, "EarthMapCreator.json");
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Failed to load configuration - Using defaults");
            Mod.Logger.Error(e);
            config = new Config();
        }
    }

    private void LoadMapLayers(ICoreAPI api) {
        var folder = api.GetOrCreateDataPath("EarthMap");
        Mod.Logger.Notification(folder);
        Layers = new MapLayers(folder);
    }
}
