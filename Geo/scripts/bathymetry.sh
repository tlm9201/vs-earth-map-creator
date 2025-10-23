#https://dap.ceda.ac.uk/bodc/gebco/global/gebco_2025/sub_ice_topography_bathymetry/geotiff/gebco_2025_sub_ice_topo_geotiff.zip
BATHY_DIR=$WORK_DIR/bathy
mkdir -p $BATHY_DIR

log "Getting bathymetry data"

get_local_dataset gebco_2025_sub_ice_topo_geotiff.zip $BATHY_DIR/.
cd $BATHY_DIR

if [[ ! -f $BATHY_DIR/gebco_2025_sub_ice_topo_geotiff.zip ]]; then
  url="https://dap.ceda.ac.uk/bodc/gebco/global/gebco_2025/sub_ice_topography_bathymetry/geotiff/gebco_2025_sub_ice_topo_geotiff.zip"
  log "Bathymetry data not found. Downloading..."
  download_file $url gebco_2025_sub_ice_topo_geotiff.zip
fi

if [[ ! -f $BATHY_DIR/gebco_2025_sub_ice_topo_geotiff.zip ]]; then
  log "CRITICAL ERROR: failed to download bathymetry"
  abort_duetoerror_cleanup $VSERR_NO_BATHYMETRY
fi

save_dataset_locally gebco_2025_sub_ice_topo_geotiff.zip

log "Unzipping bathymetry data..."
unzip gebco_2025_sub_ice_topo_geotiff.zip

log "Processing"

gdalwarp -multi -co NUM_THREADS=ALL_CPUS -wo NUM_THREADS=ALL_CPUS --config GDAL_CACHEMAX $GDAL_CACHEMAX -wm $GDAL_WM -te $LON_MIN_FINAL $LAT_MIN_FINAL $LON_MAX_FINAL $LAT_MAX_FINAL $output_projection -r cubicspline -te_srs $bbox_srs -co COMPRESS=lzw -co predictor=2 -co BIGTIFF=YES -ot Int16 $BATHY_DIR/*.tif crop.tif

pre_max_depth=$(gdalinfo crop.tif -stats | sed -n -e 's/^.*STATISTICS_MINIMUM=-//p')

gdal_calc.py -A crop.tif --co="COMPRESS=lzw" --co="predictor=2" --co="BIGTIFF=YES" --NoDataValue=-32768 --format="gtiff" --calc="(A<=0) * ((((abs(A)*$BATHY_PADDING)/$BATHY_PADDING_THRESHOLD)*(abs(A)<=$BATHY_PADDING_THRESHOLD)) + ((abs(A)>$BATHY_PADDING_THRESHOLD)*$BATHY_PADDING) + ((abs(A)>$BATHY_PADDING_THRESHOLD)*(abs(A)/$pre_max_depth)*$MAX_BATHY_DEPTH))" --outfile="$WORK_DIR/cropped_bathy.tif"

log "Bathymetry DONE"
