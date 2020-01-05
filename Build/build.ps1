
param([string]$branch = "master")

$consoleColor = "Yellow"
$scriptName = $MyInvocation.MyCommand 
$currentDir = $PSScriptRoot

Set-Location ..

$year = Get-Date -Format yy
$day = (Get-Date).DayOfYear
$version = & git rev-list --count $branch
$fullVersion = "1.$year.$day.$version"

Write-Host "version: $version" -ForegroundColor $consoleColor
Write-Host "Full version: $fullVersion" -ForegroundColor $consoleColor

$projFiles = Get-Childitem -Path $currentDir\.. -Include *.csproj -Recurse

foreach($projFile in $projFiles)
{
    Write-Host "Writing version $fullVersion in $projFile" -ForegroundColor $consoleColor

    [xml]$projXml = Get-Content -Path $projFile
    $versionNode = $projXml.Project.PropertyGroup.AssemblyVersion
    Write-Host "current $versionNode"
	$projXml.Project.PropertyGroup.AssemblyVersion = $fullVersion
    $projXml.Save($projFile)
    git add $projFile
}

git commit -m "build to publish $fullVersion" 
git tag -a $fullVersion -m "publish $fullVersion"
git push origin $branch --follow-tags

Set-Location $currentDir