#!/bin/bash

cd bin/Debug/net8.0/
mkdir -p coastline
cd coastline
rm coastline*
[[ $(curl 'https://www.ngdc.noaa.gov/mgg/shorelines/data/gshhg/latest/') =~ gshhg-shp-[[:digit:]]+.[[:digit:]]+.[[:digit:]]+.zip ]]
filename=${BASH_REMATCH[0]}
wget "https://www.ngdc.noaa.gov/mgg/shorelines/data/gshhg/latest/$filename"
unzip $filename
rm *.TXT COPYING.LESSERv3 "$filename"
mapshaper -i GSHHS_shp/h/*.shp snap combine-files -merge-layers -info -o coastline_h.shp
mapshaper -i GSHHS_shp/i/*.shp snap combine-files -merge-layers -info -o coastline_i.shp
mapshaper -i GSHHS_shp/c/*.shp snap combine-files -merge-layers -info -o coastline_c.shp
rm -r GSHHS_shp WDBII_shp
