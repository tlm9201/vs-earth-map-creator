ramsize_mb=$(($(getconf _PHYS_PAGES) * $(getconf PAGE_SIZE) / (1024 * 1024)))
if [[ $ramsize_mb -lt 4096 ]]; then export GDAL_CACHEMAX=256;fi
if [[ $ramsize_mb -ge 4096 ]]; then export GDAL_CACHEMAX=512;fi
if [[ $ramsize_mb -ge 8192 ]]; then export GDAL_CACHEMAX=1024;fi
if [[ $ramsize_mb -ge 16384 ]]; then export GDAL_CACHEMAX=2048;fi
if [[ $ramsize_mb -ge 24576 ]]; then export GDAL_CACHEMAX=4096;fi
if [[ $ramsize_mb -ge 29768 ]]; then export GDAL_CACHEMAX=8192;fi

if [[ $ramsize_mb -lt 8192 ]]; then export max_parallel_gdal=2;fi
if [[ $ramsize_mb -ge 8192 ]]; then export max_parallel_gdal=3;fi
if [[ $ramsize_mb -ge 16384 ]]; then export max_parallel_gdal=4;fi
if [[ $ramsize_mb -ge 24576 ]]; then export max_parallel_gdal=5;fi
if [[ $ramsize_mb -ge 29768 ]]; then export max_parallel_gdal=7;fi

if [[ -z $tile_width ]] && [[ -z $tile_height ]]; then
	if [[ $ramsize_mb -lt 8192 ]]; then export tile_width=4096;export tile_height=4096;fi
	if [[ $ramsize_mb -ge 8192 ]]; then export tile_width=8192;export tile_height=8192;fi
	if [[ $ramsize_mb -ge 16384 ]]; then export tile_width=16384;export tile_height=16384;fi
	if [[ $ramsize_mb -ge 24576 ]]; then export tile_width=16384;export tile_height=16384;fi
	if [[ $ramsize_mb -ge 29768 ]]; then export tile_width=16384;export tile_height=16384;fi
fi

export max_parallel_wp_export=1

log "RAM available: $ramsize_mb"
log "Auto max tile size: $tile_width x $tile_height"
log "Auto max parallel tiles gdal: $max_parallel_gdal"
log "Auto GDAL Cachemax: $GDAL_CACHEMAX"

export GDAL_VRT_ENABLE_PYTHON=YES
export GDAL_NUM_THREADS=ALL_CPUS
export GDAL_WM="1024"
export VRT_SHARED_SOURCE=1
