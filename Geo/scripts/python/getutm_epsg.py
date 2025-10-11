import sys
from pyproj import CRS
from pyproj.aoi import AreaOfInterest
from pyproj.database import query_utm_crs_info

latmin=float(sys.argv[1])
latmax=float(sys.argv[2])
lonmin=float(sys.argv[3])
lonmax=float(sys.argv[4])

latcenter=((latmax+latmin)/2)
loncenter=((lonmax+lonmin)/2)

if((lonmax-lonmin)<13):
    utm_crs_list = query_utm_crs_info(
        datum_name="WGS 84",
        area_of_interest=AreaOfInterest(
            west_lon_degree=loncenter,
            south_lat_degree=latcenter,
            east_lon_degree=loncenter,
            north_lat_degree=latcenter,
        ),
    )
    utm_crs = CRS.from_epsg(utm_crs_list[0].code)
    print(utm_crs)
else:
    t_srs="EPSG:3857"
    print(t_srs)
