$SvgFiles=Get-ChildItem "./*.svg"
foreach ($SvgFile in $SvgFiles) {
  #"SvgFile="+$SvgFile
  $FilePathWoExt=[System.IO.Path]::GetFileNameWithoutExtension($SvgFile.Name)
  foreach ($Size in ( 0 )) {
    try {
      $Appendix=""
      $InkscapeOptions="";
      if ($Size -gt 0) {
        $Appendix=""+$Size+"x"+$Size
        $InkscapeOptions="--export-width="+$Size
      }
      #"Appendix="+$Appendix
      $PngFilePath=$FilePathWoExt+$Appendix+".png"
      $DoConvert=0
      if (!(Test-Path $PngFilePath)) {
        $DoConvert=1;
      } elseif ($SvgFile.LastWriteTime -gt (ls $PngFilePath).LastWriteTime) {
        $DoConvert=1;
      }
      if ($DoConvert -gt 0) {
        "rem "+$SvgFile.Name+" is newer than "+$PngFilePath
        "rem Inkscape "+$SvgFile.Name+" --without-gui "+$InkscapeOptions+" --export-png="+$PngFilePath
        & 'C:\Program Files\Inkscape\inkscape.exe' $SvgFile.Name --without-gui $InkscapeOptions --export-png=$PngFilePath
      }
    } catch {
      "Exception: "+$_.Exception.Message
    }
  }
}
#pause
