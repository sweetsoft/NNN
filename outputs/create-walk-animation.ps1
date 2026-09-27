$ErrorActionPreference = 'Stop'
$root = 'E:/Work/Sweet/NNN_View'
$texture = "$root/Assets/NNN/Texture/walk_all.png"
$clipPath = "$root/Assets/NNN/Animation/Cat_Walk.anim"
if ((Test-Path "$texture.meta") -or (Test-Path $clipPath)) { throw 'Destination assets already exist; inspect before overwriting.' }
Add-Type -AssemblyName System.Drawing
$bitmap = [System.Drawing.Bitmap]::new($texture)
$bands = @(@(233,401),@(566,719),@(880,1028))
$frames = @()
for ($row=0; $row -lt 3; $row++) {
  for ($col=0; $col -lt 4; $col++) {
    $left = [int][Math]::Floor($col*$bitmap.Width/4.0)
    $right = [int][Math]::Floor(($col+1)*$bitmap.Width/4.0)
    $count=0
    for($y=$bands[$row][0];$y -le $bands[$row][1];$y++) {
      for($x=$left;$x -lt $right;$x++) { if($bitmap.GetPixel($x,$y).A -gt 0){$count++} }
    }
    if($count -lt 100){continue}
    $index=$frames.Count
    $frames += [pscustomobject]@{Name=('walk_all_{0:D2}' -f $index); Id=(21300000+2*$index); X=$left; Y=($bitmap.Height-$bands[$row][1]-1); Width=($right-$left); Height=180; SpriteId=([guid]::NewGuid().ToString('N')); Pixels=$count}
  }
}
$bitmap.Dispose()
if($frames.Count -ne 10){throw "Expected 10 frames, got $($frames.Count)"}
$guid = [guid]::NewGuid().ToString('N')
$meta = Get-Content "$root/Assets/NNN/Texture/cat01_00.png.meta" -Raw
$meta = $meta -replace '(?m)^guid: .*', "guid: $guid"
$meta = $meta -replace 'spriteMode: 1','spriteMode: 2'
$meta = $meta -replace 'spriteMeshType: 1','spriteMeshType: 0'
$meta = $meta -replace 'textureCompression: 1','textureCompression: 0'
$meta = $meta -replace 'spriteGenerateFallbackPhysicsShape: 1','spriteGenerateFallbackPhysicsShape: 0'
$ids = "  internalIDToNameTable:`n" + (($frames | ForEach-Object {"  - first:`n      213: $($_.Id)`n    second: $($_.Name)"}) -join "`n")
$meta = $meta.Replace('  internalIDToNameTable: []',$ids)
$sprites = "    sprites:`n" + (($frames | ForEach-Object {@"
    - serializedVersion: 2
      name: $($_.Name)
      rect:
        serializedVersion: 2
        x: $($_.X)
        y: $($_.Y)
        width: $($_.Width)
        height: $($_.Height)
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: $($_.SpriteId)
      internalID: $($_.Id)
      vertices: []
      indices: 
      edges: []
      weights: []
      customData: 
"@}) -join "`n")
$meta=$meta.Replace('    sprites: []',$sprites)
$table="    nameFileIdTable:`n"+(($frames | ForEach-Object {"      $($_.Name): $($_.Id)"}) -join "`n")
$meta=$meta.Replace('    nameFileIdTable: {}',$table)
$clip = Get-Content "$root/Assets/NNN/Animation/Cat_Sit.anim" -Raw
$clip = $clip.Replace('m_Name: Cat_Sit','m_Name: Cat_Walk')
$culture=[Globalization.CultureInfo]::InvariantCulture
$keys=@(); $mapping=@()
for($i=0;$i -lt $frames.Count;$i++){
  $time=($i/12.0).ToString('0.#########',$culture)
  $ref="{fileID: $($frames[$i].Id), guid: $guid, type: 3}"
  $keys += "    - time: $time`n      value: $ref"
  $mapping += "    - $ref"
}
$clip=[regex]::Replace($clip,'(?s)(    curve:\s*\n).*?(    attribute:)',{param($m) $m.Groups[1].Value+($keys -join "`n")+"`n"+$m.Groups[2].Value})
$clip=[regex]::Replace($clip,'(?s)(    pptrCurveMapping:\s*\n).*?(  m_AnimationClipSettings:)',{param($m) $m.Groups[1].Value+($mapping -join "`n")+"`n"+$m.Groups[2].Value})
$clip=$clip -replace 'm_StopTime: .*','m_StopTime: 0.833333333'
[IO.File]::WriteAllText("$texture.meta",$meta)
[IO.File]::WriteAllText($clipPath,$clip)
$clipGuid=[guid]::NewGuid().ToString('N')
[IO.File]::WriteAllText("$clipPath.meta", "fileFormatVersion: 2`nguid: $clipGuid`nNativeFormatImporter:`n  externalObjects: {}`n  mainObjectFileID: 7400000`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n")
$frames | Format-Table Name,Id,X,Y,Width,Height,Pixels
Write-Output "Created $clipPath (10 frames, 12 fps, loop)."

