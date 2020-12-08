param (
	[string]$tag
)

if (!$tag) {
	Write-Host "Tag parameter is required"
	exit 1
}
$tagMatch = $tag -match "^v*\d\.\d\.\d\.\d"
if (!$tagMatch) {
	Write-Host "Tag must be in v#.#.#.# format"
	exit 1
}

$adjustedTag = $tag -replace "v",""
$projectFileContents = Get-Content AzureDevOpsMigrator.WPF\AzureDevOpsMigrator.WPF.csproj
$updatedProjectFileContents = $projectFileContents -replace "<FileVersion>\d\.\d\.\d\.\d<\/FileVersion>","<FileVersion>$adjustedTag</FileVersion>"
Write-Host $updatedProjectFileContents

$updatedProjectFileContents | Set-Content AzureDevOpsMigrator.WPF\AzureDevOpsMigrator.WPF.csproj 

dotnet publish AzureDevOpsMigrator.WPF\AzureDevOpsMigrator.WPF.csproj -c Release /p:PublishProfile=AzureDevOpsMigrator.WPF\Properties\PublishProfiles\FolderProfile.pubxml

Write-Host "Awaiting file lock release.."
Start-Sleep -s 1
Write-Host "Continuing to zip outputs.."
$flatTag = $tag -replace '\.','_'
Compress-Archive -Path AzureDevOpsMigrator.WPF\bin\publish\*.* -DestinationPath "AzureDevOpsMigrator.WPF\bin\publish\AzureDevOpsMigrationUtility_$flatTag.zip"
Write-Host "Done!"