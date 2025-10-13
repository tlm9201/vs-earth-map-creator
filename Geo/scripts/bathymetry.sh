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
# save_dataset_locally "gebco_2025_sub_ice_topo.zip"

log "Unzipping bathymetry data"
# unzip -o "gebco_2025_sub_ice_topo.zip"

log "Processing bathymetry data"

# Step 1: Crop the global data
gdalwarp -multi -co NUM_THREADS=ALL_CPUS -wo NUM_THREADS=ALL_CPUS \
    --config GDAL_CACHEMAX $GDAL_CACHEMAX -wm $GDAL_WM \
    -te $LON_MIN_FINAL $LAT_MIN_FINAL $LON_MAX_FINAL $LAT_MAX_FINAL \
    $output_projection -r cubicspline -te_srs $bbox_srs \
    -co COMPRESS=lzw -co PREDICTOR=2 -co BIGTIFF=YES -ot Int16 \
    NETCDF:"GEBCO_2025_sub_ice.nc":elevation "crop.tif"

# Step 2: Calculate the final raster
log "Scaling bathymetry data..."

# Get the true minimum depth from the cropped file.
stats=$(gdalinfo -mm crop.tif | tr ',' '.')
min_val=$(echo "$stats" | grep "Computed Min/Max" | cut -d "," -f 1 | cut -d "=" -f 2 | tr -d ' ')
log "Minimum depth (min_val) in this area is $min_val meters."

# Prevent errors if there is no ocean in the scene
if (( $(echo "$min_val >= 0" | bc -l) )); then
    min_val=-1
fi

# Define the target RGB values from config
to_high=$BATHY_SCALE_SEALEVEL # e.g., 110 (value for sea level)
to_low=$BATHY_SCALE_MAXDEPTH  # e.g., 50 (value for deepest point)

# Pre-calculate the absolute value of min_val to simplify the formula
# and avoid shell parsing errors with double negatives.
abs_min_val=$(echo "$min_val" | awk '{print $1 * -1}')

# This simplified formula maps the raw depth [min_val -> 0] to the target RGB range [to_low -> to_high].
# It is mathematically identical to the previous version but is much less likely to be misinterpreted by the shell.
# (A + abs_min_val) gives the depth from the bottom, which we then normalize.
scaling_logic="((A + $abs_min_val) / ($abs_min_val * 1.0)) * ($to_high - $to_low) + $to_low"
calc_expr="where(A < 0, $scaling_logic, 0)"

log "DEBUG: Scaling variables -> abs_min_val=$abs_min_val, to_high=$to_high, to_low=$to_low"
log "DEBUG: Full calculation string -> $calc_expr"

gdal_calc.py -A crop.tif --outfile="$WORK_DIR/bathymetry/cropped_bathy.tif" \
    --calc="$calc_expr" \
    --NoDataValue=0 --co="COMPRESS=LZW" --co="PREDICTOR=2" --type='Byte' --overwrite

log "Bathymetry processing DONE"
