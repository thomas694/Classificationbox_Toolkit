# Harvest Data
$harvestPath = "$env:APPVEYOR_BUILD_FOLDER\src\imgclass.net\bin\Release"
$harvestPath2 = "$env:APPVEYOR_BUILD_FOLDER\src\Classificationbox.Net\bin\Release\netstandard2.0"
$fileVersion = (Get-Item "$harvestPath\ImgClass.Net.exe").VersionInfo.ProductVersion
[VERSION]$vs = $fileVersion -replace '^.+((\d+\.){3}\d+).+', '$1'
$version = '{0}.{1}.{2}' -f $vs.Major,$vs.Minor,$vs.Build

# Artifacts Paths
$artifactsPath = "$env:APPVEYOR_BUILD_FOLDER\artifacts"
$applicationArtifactsPath = "$artifactsPath\Classificationbox_Toolkit"

New-Item -ItemType Directory -Force -Path $applicationArtifactsPath

# Copy in Application Artifacts
Get-ChildItem -Path "$harvestPath\*" -Include *.exe,*.dll,*.config | Copy-Item -Destination $applicationArtifactsPath
Get-ChildItem -Path "$harvestPath2\*" -Include *.dll,*.pdb,*.json | Copy-Item -Destination $applicationArtifactsPath

# Zip Application
$applicationZipPath = "$artifactsPath\Classificationbox_Toolkit-v$version.zip"
Compress-Archive -Path "$artifactsPath\Classificationbox_Toolkit\" -DestinationPath "$applicationZipPath"
