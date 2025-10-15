cd $WORK_DIR

BUILD_DIR=$WORK_DIR/build

mkdir -p $BUILD_DIR

#land_osm_mask.tif
#cropped_dem.tif

GDAL_TRANSLATE="gdal_translate"

if [ $RESIZE_MAP -eq 1 ]; then
  echo "Resizing map"
  GDAL_TRANSLATE="$GDAL_TRANSLATE -outsize $FINAL_WIDTH $FINAL_LENGTH"
fi

# land mask
eval $GDAL_TRANSLATE -ot Byte land_osm_mask.tif $BUILD_DIR/landmask.png

# real max is 8718
eval $GDAL_TRANSLATE -expand RGB -scale 0.0 8718 0 $VINTAGE_STORY_BUILD_LIMIT -ot Byte cropped_dem.tif $BUILD_DIR/heightmap.png

# climate
eval $GDAL_TRANSLATE -ot Byte climate.tif $BUILD_DIR/climate.png

# tree
eval $GDAL_TRANSLATE -ot Byte tree.tif $BUILD_DIR/tree.png

# river
eval $GDAL_TRANSLATE -ot Byte rivers.tif $WORK_DIR/river.png
magick $WORK_DIR/river.png -channel RGB -threshold 90% -blur 0x5 -posterize 10 -level 0%,100%,1.0 $BUILD_DIR/river.png
