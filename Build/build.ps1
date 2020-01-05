
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
#$projFiles | ForEach-Object { Write-Host $_ -ForegroundColor $consoleColor }

foreach($projFile in $projFiles)
{
    Write-Host "Writing version $version in $projFile" -ForegroundColor $consoleColor

    $xml.Load($projFile)
	$versionNode = $xml.Project.PropertyGroup.Version
    if ($null -eq $versionNode) 
    {
		$versionNode = $xml.CreateElement("Version")
		$xml.Project.PropertyGroup.AppendChild($versionNode)
		Write-Host "Version XML tag added to the csproj"
	}
	$xml.Project.PropertyGroup.Version = $fullVersion
	$xml.Save($projFile)
}

#git tag -a $fullVersion -m "publish $fullVersion"
#git push origin $branch --follow-tags

Set-Location $currentDir