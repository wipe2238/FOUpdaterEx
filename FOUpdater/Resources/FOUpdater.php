<?php

$directory = '.';

$ignore = array(
	'FOUpdater.php'
);

$always = array(
//	'd3dx9_42.dll'
);

$norewrite = array(
	'FOnline.cfg',
	'FOnlineGL.cfg',
	'IgnoreList.txt',
	'NameColorizing.txt',
	'data/cache/default.cache',
);

$delete = array(
);

$config = array(
);

$updater = array(
	'Updater.exe',
	'UpdaterConsole.exe'
);

$html_border1 = "solid";
$html_border2 = "ridge";


if( $_SERVER['QUERY_STRING'] == "foupdater" )
	$foupdater = true;
else
{
	$foupdaterAgent = "FOUpdater/";
	$foupdater = (substr($_SERVER['HTTP_USER_AGENT'], 0, strlen($foupdaterAgent)) === $foupdaterAgent);
}

function listdir( $dir, &$files, &$ignore )
{
    $handle = opendir($dir);
    while( ($file = readdir($handle)) !== false )
	{
		if( $file == '.' || $file == '..' )
			continue;

		if( in_array( "$file/", $ignore ))
			continue;

		if( in_array( $file, $ignore ))
			continue;

		$filepath = $dir == '.' ? $file : $dir . '/' . $file;

		if( is_link( $filepath ))
			continue;

		if( is_file( $filepath ))
			$files[] = $filepath;

		else if( is_dir( $filepath ))
			listdir( $filepath, $files, $ignore );
    }
    closedir( $handle );
}

/*
 * Aidan Lister <aidan@php.net>
 * http://aidanlister.com/2004/04/human-readable-file-sizes/
 */
function size_readable($size, $max = null, $system = 'si', $retstring = '%01.2f %s')
{
    // Pick units
    $systems['si']['prefix'] = array('B', 'K', 'MB', 'GB', 'TB', 'PB');
    $systems['si']['size']   = 1000;
    $systems['bi']['prefix'] = array('B', 'KB', 'MB', 'GB', 'TB', 'PB');
    $systems['bi']['size']   = 1024;
    $sys = isset($systems[$system]) ? $systems[$system] : $systems['si'];

    $depth = count($sys['prefix']) - 1;
    if ($max && false !== $d = array_search($max, $sys['prefix'])) {
	$depth = $d;
    }

    $i = 0;
    while ($size >= $sys['size'] && $i < $depth) {
	$size /= $sys['size'];
	$i++;
    }

    return sprintf($retstring, $size, $sys['prefix'][$i]);
}

/*
 * Toni
 * http://stackoverflow.com/a/2161310
 */

function winVersion($FileName)
{
    $handle=fopen($FileName,'rb');
    if (!$handle) return FALSE;
    $Header=fread ($handle,64);
    if (substr($Header,0,2)!='MZ') return FALSE;
    $PEOffset=unpack("V",substr($Header,60,4));
    if ($PEOffset[1]<64) return FALSE;
    fseek($handle,$PEOffset[1],SEEK_SET);
    $Header=fread ($handle,24);
    if (substr($Header,0,2)!='PE') return FALSE;
    $Machine=unpack("v",substr($Header,4,2));
    if ($Machine[1]!=332) return FALSE;
    $NoSections=unpack("v",substr($Header,6,2));
    $OptHdrSize=unpack("v",substr($Header,20,2));
    fseek($handle,$OptHdrSize[1],SEEK_CUR);
    $ResFound=FALSE;
    for ($x=0;$x<$NoSections[1];$x++) {
	$SecHdr=fread($handle,40);
	if (substr($SecHdr,0,5)=='.rsrc') { // resource section
	    $ResFound=TRUE;
	    break;
	}
    }
    if (!$ResFound) return FALSE;
    $InfoVirt=unpack("V",substr($SecHdr,12,4));
    $InfoSize=unpack("V",substr($SecHdr,16,4));
    $InfoOff=unpack("V",substr($SecHdr,20,4));
    fseek($handle,$InfoOff[1],SEEK_SET);
    $Info=fread($handle,$InfoSize[1]);
    $NumDirs=unpack("v",substr($Info,14,2));
    $InfoFound=FALSE;
    for ($x=0;$x<$NumDirs[1];$x++) {
	$Type=unpack("V",substr($Info,($x*8)+16,4));
	if($Type[1]==16) { // FILEINFO resource
	    $InfoFound=TRUE;
	    $SubOff=unpack("V",substr($Info,($x*8)+20,4));
	    break;
	}
    }
    if (!$InfoFound) return FALSE;
    $SubOff[1]&=0x7fffffff;
    $InfoOff=unpack("V",substr($Info,$SubOff[1]+20,4)); // offset of first FILEINFO
    $InfoOff[1]&=0x7fffffff;
    $InfoOff=unpack("V",substr($Info,$InfoOff[1]+20,4)); // offset to data
    $DataOff=unpack("V",substr($Info,$InfoOff[1],4));
    $DataSize=unpack("V",substr($Info,$InfoOff[1]+4,4));
    $CodePage=unpack("V",substr($Info,$InfoOff[1]+8,4));
    $DataOff[1]-=$InfoVirt[1];
    $Version=unpack("v4",substr($Info,$DataOff[1]+48,8));
    $x=$Version[2];
    $Version[2]=$Version[1];
    $Version[1]=$x;
    $x=$Version[4];
    $Version[4]=$Version[3];
    $Version[3]=$x;
    return $Version;
}

if( $_SERVER['QUERY_STRING'] == "foupdater" )
	$foupdater = true;
else
{
	$foupdaterAgent = "FOUpdater/";
	$foupdater = (substr($_SERVER['HTTP_USER_AGENT'], 0, strlen($foupdaterAgent)) === $foupdaterAgent);
}


$files = array();
listdir( $directory, $files, $ignore );
$files = array_unique( array_merge( $files, $delete ));
sort( $files, SORT_REGULAR );


if( $foupdater )
    header( "Content-Type: text/plain" );
else
{
    header( "Content-Type: text/html" );
    print( "<!DOCTYPE HTML>
<html>
\t<head>
\t\t<title>FOUpdater info</title>
\t</head>
\t<body>
\t\t<table style=\"margin:auto; border:$html_border1;\">
\t\t\t<tr id=\"files\"><th style=\"border:$html_border2;\">CRC</th><th style=\"border:$html_border2;\">Size</th><th style=\"border:$html_border2;\">Filename</th><th style=\"border:$html_border2;\">Options</th><th style=\"border:$html_border2;\">Version</th></tr>\n"
    );
};

foreach( $files as $file )
{
	set_time_limit(30);

	if( in_array( $file, $updater ))
		continue;

	$crc = (file_exists($file) ? crc32( file_get_contents( $file )) : 0);
	$size = (file_exists($file) ? filesize( $file ) : 0);
	$options = array();

	if( in_array( $file, $always ))
		array_push( $options, "always" );

	if( in_array( $file, $delete ))
		array_push( $options, "delete" );

	if( in_array( $file, $norewrite ))
		array_push( $options, "norewrite" );

	$version = (file_exists($file) ? winVersion( $file ) : '');
	if( count($version) == 4 )
	    $version = implode( '.', $version );
	else
	    $version = '';

	$exists = file_exists( $file );

	if( $foupdater )
	{
		printf( "%u %d %s",
			in_array( "delete", $options ) ? 0 : $crc,
			in_array( "delete", $options ) ? 0 : $size,
			str_replace( " ", "%20", $file )
		);
	
		if( count($options) > 0 )
		{
		    printf( " %s", implode( ",", $options ));
		}
	}
	else
	{
		printf( "\t\t\t<tr><td>%s</td><td>%s</td><td>%s</td><td>%s</td><td>%s</td></tr>",
			in_array( "delete", $options ) ? '' : $crc,
			in_array( "delete", $options ) ? '' : size_readable( $size, null, 'bi' ),
			($exists ? "<a href=\"" . str_replace( " ", "%20", $file ) . "\">" : '') . $file . ($exists ? "</a>" : ''),
			implode( ", ", $options ),
			$version
		);
	}

	printf( "\n" );
}

$th = false;
foreach( $config as $line )
{
	$cfg = explode( "=", $line );
	if( count($cfg) != 2 )
		continue;

	if( $foupdater )
	{
		printf( "config %s %s\n",
		    $cfg[0],
		    $cfg[1]
		);
	}
	else
	{
		if( !$th )
		{
			printf( "\t\t\t<tr><td>&nbsp;</td></tr>\n" );
			printf( "\t\t\t<tr id=\"config\"><th style=\"border:$html_border2;\" colspan=\"2\">Setting</th><th style=\"border:$html_border2;\" colspan=\"3\">Value</th></tr>\n" );
			$th = true;
		}
		printf( "\t\t\t<tr><td colspan=\"2\">%s</td><td colspan=\"3\">%s</th></tr>\n",
			$cfg[0],
			$cfg[1]
		);
	}
}

$th = false;
foreach( $updater as $file )
{
	if( !file_exists($file) )
		continue;

	$size = filesize( $file );
	$version = (file_exists($file) ? winVersion( $file ) : '');
	if( count($version) == 4 )
	    $version = implode( '.', $version );
	else
	    $version = '';

	if( $foupdater )
	{
		printf( "updater %d %s %s\n",
			$size,
			str_replace( " ", "%20", $file ),
			$version
		);
	}
	else
	{
		if( !$th )
		{
			printf( "\t\t\t<tr><td>&nbsp;</td></tr>\n" );
			printf( "\t\t\t<tr id=\"updater\"><th style=\"border:$html_border2;\" colspan=\"2\">Size</th><th style=\"border:$html_border2;\">Filename</th><th style=\"border:$html_border2;\" colspan=\"2\">Version</tr>\n" );
			$th = true;
		}
		printf( "\t\t\t<tr><td colspan=\"2=\">%s</td><td>%s</td><td colspan=\"2\">%s</td></tr>\n",
		    size_readable( $size ),
		    "<a href=\"" . str_replace( " ", "%20", $file ) . "\">" . $file . "</a>",
		    $version
		);
	}
}


if( !$foupdater )
{
	printf( "\t\t\t<tr><td>&nbsp;</td></tr>\n" );
	printf( "\t\t\t<tr><td style=\"border:$html_border2; text-align:center;\" colspan=\"5\"><a href=\"%s\">Raw version</a></td></tr>
\t\t</table>
\t</body>
</html>\n",
	    $_SERVER['REQUEST_URI'] . "?foupdater"
	);
};

?>
