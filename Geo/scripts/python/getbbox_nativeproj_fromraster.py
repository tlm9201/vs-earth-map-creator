import sys
from osgeo import gdal
tiff_file_path = sys.argv[1]

gdal.UseExceptions()

ds = gdal.Open(tiff_file_path)
width = ds.RasterXSize
height = ds.RasterYSize
gt = ds.GetGeoTransform()
minx = gt[0]
miny = gt[3] + width*gt[4] + height*gt[5]
maxx = gt[0] + width*gt[1] + height*gt[2]
maxy = gt[3]
print(f"{width} {height} {minx} {miny} {maxx} {maxy}")
