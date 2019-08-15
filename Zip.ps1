Get-ChildItem -Include *IronstoneDraughter.dll -Exclude *Tests.dll -Recurse | Compress-Archive -Update -DestinationPath ($PSScriptRoot + "\IronstoneDraughter")
Write-Host "Zip file created"