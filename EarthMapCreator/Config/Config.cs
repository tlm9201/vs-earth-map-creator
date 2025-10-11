namespace EarthMapCreator;

public class Config {
    public int MapWidthBlocks = 1024;
    public int MapHeightBlocks = 512;
    
    // rivers
    public int RiverDepth = 20;
    
    // gen modding
    public byte PrecipitationAdd = 0;
    public byte TemperatureAdd = 0;
    public byte ForestAdd = 0;
    
    public double PrecipitationMulti = 1.0;
    public double TemperatureMulti = 1.0;
    public double ForestMulti = 1.0;
}