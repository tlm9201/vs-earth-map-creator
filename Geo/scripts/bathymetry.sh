log "Starting Bathymetry step"

BATHY_DIR="$WORK_DIR/bathymetry"
mkdir -p "$BATHY_DIR"

log "Downloading GEBCO 2025 data, or using locally stored file"
url="https://dap.ceda.ac.uk/bodc/gebco/global/gebco_2025/sub_ice_topography_bathymetry/netcdf/gebco_2025_sub_ice_topo.zip?download=1"

get_local_dataset "gebco_2025_sub_ice_topo.zip" $BATHY_DIR/.
if [[ ! -f $BATHY_DIR/gebco_2025_sub_ice_topo.zip ]]; then
  log "Local GEBCO data not found. Downloading... (This is a ~1.2 GB file and may take a while)"
  curl -C - --connect-timeout 10 --retry 60 --retry-delay 10 --retry-all-errors -L "$url" -o $BATHY_DIR/gebco_2025_sub_ice_topo.zip
fi

cd $BATHY_DIR
save_dataset_locally "gebco_2025_sub_ice_topo.zip"

log "Unzipping bathymetry data"
unzip -o "gebco_2025_sub_ice_topo.zip"

log "Processing bathymetry data"

# Step 1: Crop the global data
gdalwarp -multi -co NUM_THREADS=ALL_CPUS -wo NUM_THREADS=ALL_CPUS \
    --config GDAL_CACHEMAX $GDAL_CACHEMAX -wm $GDAL_WM \
    -te $LON_MIN_FINAL $LAT_MIN_FINAL $LON_MAX_FINAL $LAT_MAX_FINAL \
    $output_projection -r cubicspline -te_srs $bbox_srs \
    -co COMPRESS=lzw -co PREDICTOR=2 -co BIGTIFF=YES -ot Int16 \
    NETCDF:"GEBCO_2025_sub_ice.nc":elevation "crop.tif"

# Step 2: Calculate the final raster
log "Scaling bathymetry data to the final output range..."

# Get the statistics from the cropped file to find the true min/max depth
stats=$(gdalinfo -mm crop.tif | tr ',' '.')
min_val=$(echo "$stats" | grep "Computed Min/Max" | cut -d "," -f 1 | cut -d "=" -f 2 | tr -d ' ')

# Max depth is the absolute value of the minimum value (e.g., -6000m -> 6000)
# Use awk for floating-point multiplication
max_depth=$(echo "$min_val" | awk '{print $1 * -1}')
log "Maximum depth in this area is $max_depth meters."

# Prevent division by zero if there is no ocean in the crop
if (( $(echo "$max_depth <= 0" | bc -l) )); then
    max_depth=1
fi

# Define the sea level and max depth values from config
sea_level_val=$BATHY_SCALE_SEALEVEL
max_depth_val=$BATHY_SCALE_MAXDEPTH

# where A is ocean (A<0), apply the scaling formula; otherwise, the value is 0.
scaling_logic="$sea_level_val + (A * -1) * ($max_depth_val - $sea_level_val) / ($max_depth * 1.0)"
calc_expr="where(A < 0, $scaling_logic, 0)"

gdal_calc.py -A crop.tif --outfile="$WORK_DIR/bathymetry/cropped_bathy.tif" \
    --calc="$calc_expr" \
    --NoDataValue=0 --co="COMPRESS=LZW" --co="PREDICTOR=2" --type='Byte' --overwrite

log "Bathymetry processing DONE"