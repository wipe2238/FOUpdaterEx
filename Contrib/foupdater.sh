#!/bin/sh
# bash FOUpdater
# by jan0s1k
# http://jan0s1k.fode.eu/fonline/updater.sh
# http://fonline2238.net/forum/index.php/topic,26023.msg246047.html#msg246047

VERSION="0.1.0"
SERVER="http://fonline2238.net/update/"
FOD=$(pwd)
wget -O index "$SERVER" --user-agent="FOUpdater/$VERSION"
LINES=`cat index | wc -l`
for i in `seq 1 $LINES`
do
 FILE=`cat index | awk "NR==$i" |awk '{print $3;}'`
 CRC=`cat index | awk "NR==$i" |awk '{print $1;}'`
 NOREWRI=`cat index | awk "NR==$i" |awk '{print $4;}'`
 if [ ! -f $FOD/$FILE ] && [ $CRC != 'updater' ] && [ $CRC != 'config' ]
 then
  wget $SERVER/$FILE --force-directories -nH
 else
  if [ $CRC != 'updater' ] && [ $CRC != 'config' ] && [ $CRC != `perl -e 'use String::CRC32; open(file,$ARGV[0]); $crc=crc32(*file); close(file); printf("%u",$crc);' $FILE` ] && [ $NOREWRI != "norewrite" ]
  then
   wget -O $FILE $SERVER/$FILE
  fi
 fi
done
