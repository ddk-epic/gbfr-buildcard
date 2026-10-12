<#
    .\Publish.ps1 [-MakeDelta true] [-BuildR2R true] [-UseGitHubDelta true]
    Builds the mod and publishes its packages for the generic, NuGet and GameBanana targets.
#>
[cmdletbinding()]
param (
    $IsPrerelease=$False, 
    $MakeDelta=$False, 
    $ChangelogPath="",
    $ReadmePath="",
    $Build=$True,
    $BuildR2R=$False,
    $RemoveExe=$True,
    $UseScriptDirectory=$True,

    $ProjectPath = "gbfr.qol.buildcard.csproj",
    $PackageName = "gbfr.qol.buildcard",
    $PublishOutputDir = "Publish/ToUpload",

    # Delta source; enable one.
    $MetadataFileName = "Sewer56.Update.ReleaseMetadata.json",
    $UseGitHubDelta = $False,
    $UseGameBananaDelta = $False,
    $UseNuGetDelta = $False,

    $GitHubUserName = "",
    $GitHubRepoName = "",
    $GitHubFallbackPattern = "",
    $GitHubInheritVersionFromTag = $True,

    $GameBananaItemId = 333681, # From mod page URL.

    $NuGetPackageId = "gbfr.qol.buildcard",
    $NuGetFeedUrl = "https://packages.sewer56.moe/v3/index.json",
    $NuGetAllowUnlisted = $False,

    $PublishGeneric    = $True,
    $PublishNuGet      = $True,
    $PublishGameBanana = $True
)

$publishBuildDirectory = "Publish/Builds/CurrentVersion"
$deltaDirectory = "Publish/Builds/LastVersion"     # Last published version of the mod

$PublishGenericDirectory = "$PublishOutputDir/Generic"
$PublishNuGetDirectory   = "$PublishOutputDir/NuGet"
$PublishGameBananaDirectory = "$PublishOutputDir/GameBanana"

$reloadedToolsPath = "./Publish/Tools/Reloaded-Tools"
$updateToolsPath   = "./Publish/Tools/Update-Tools"
$reloadedToolPath = "$reloadedToolsPath/Reloaded.Publisher.exe"
$updateToolPath   = "$updateToolsPath/Sewer56.Update.Tool.dll"
$changelogFullPath = $null
$readmeFullPath = $null
if ($ChangelogPath) { $changelogFullPath = [System.IO.Path]::GetFullPath($ChangelogPath) }
if ($ReadmePath) { $readmeFullPath = [System.IO.Path]::GetFullPath($ReadmePath) }

# Set Working Directory
$UseScriptDirectory = [bool]::Parse($UseScriptDirectory)
if ($UseScriptDirectory) {
    Split-Path $MyInvocation.MyCommand.Path | Push-Location
    [Environment]::CurrentDirectory = $PWD
}

$IsPrerelease = [bool]::Parse($IsPrerelease)
$MakeDelta = [bool]::Parse($MakeDelta)
$Build = [bool]::Parse($Build)
$BuildR2R = [bool]::Parse($BuildR2R)
$RemoveExe = [bool]::Parse($RemoveExe)
$UseGitHubDelta = [bool]::Parse($UseGitHubDelta)
$UseGameBananaDelta = [bool]::Parse($UseGameBananaDelta)
$UseNuGetDelta = [bool]::Parse($UseNuGetDelta)
$NuGetAllowUnlisted = [bool]::Parse($NuGetAllowUnlisted)
$PublishGeneric = [bool]::Parse($PublishGeneric)
$PublishNuGet = [bool]::Parse($PublishNuGet)
$PublishGameBanana = [bool]::Parse($PublishGameBanana)
$GitHubInheritVersionFromTag = [bool]::Parse($GitHubInheritVersionFromTag)
$TempDirectory = [System.IO.Path]::GetTempPath() + [System.IO.Path]::GetRandomFileName()
$TempDirectoryBuild = "$TempDirectory/build"

function Get-Tools {
    $ProgressPreference = 'SilentlyContinue'
    if (-not(Test-Path -Path $reloadedToolsPath -PathType Any)) {
        Write-Host "Downloading Reloaded Tools"
        Invoke-WebRequest -Uri "https://github.com/Reloaded-Project/Reloaded-II/releases/latest/download/Tools.zip" -OutFile "$TempDirectory/Tools.zip"
        Expand-Archive -LiteralPath "$TempDirectory/Tools.zip" -DestinationPath $reloadedToolsPath

        Remove-Item "$TempDirectory/Tools.zip" -ErrorAction SilentlyContinue
    }

    if ($MakeDelta -and -not(Test-Path -Path $updateToolsPath -PathType Any)) {
        Write-Host "Downloading Update Library Tools"
        Invoke-WebRequest -Uri "https://github.com/Sewer56/Update/releases/latest/download/Sewer56.Update.Tool.zip" -OutFile "$TempDirectory/Sewer56.Update.Tool.zip"
        Expand-Archive -LiteralPath "$TempDirectory/Sewer56.Update.Tool.zip" -DestinationPath $updateToolsPath

        Remove-Item "$TempDirectory/Sewer56.Update.Tool.zip" -ErrorAction SilentlyContinue    
    }
}

function Build {
    Remove-Item $publishBuildDirectory -Recurse -ErrorAction SilentlyContinue
    New-Item $publishBuildDirectory -ItemType Directory -ErrorAction SilentlyContinue

    dotnet restore $ProjectPath
    dotnet clean $ProjectPath

    if ($BuildR2R) {
        dotnet publish $ProjectPath -c Release -r win-x86 --self-contained false -o "$publishBuildDirectory/x86" /p:PublishReadyToRun=true /p:OutputPath="$TempDirectoryBuild/x86"
        dotnet publish $ProjectPath -c Release -r win-x64 --self-contained false -o "$publishBuildDirectory/x64" /p:PublishReadyToRun=true /p:OutputPath="$TempDirectoryBuild/x64"

        # Keep one copy of the shared files
        Move-Item -Path "$publishBuildDirectory/x86/ModConfig.json" -Destination "$publishBuildDirectory/ModConfig.json" -ErrorAction SilentlyContinue
        Move-Item -Path "$publishBuildDirectory/x86/Preview.jpg" -Destination "$publishBuildDirectory/Preview.jpg" -ErrorAction SilentlyContinue
        Remove-Item "$publishBuildDirectory/x64/Preview.jpg" -ErrorAction SilentlyContinue
        Remove-Item "$publishBuildDirectory/x64/ModConfig.json" -ErrorAction SilentlyContinue
    }
    else {
        dotnet publish $ProjectPath -c Release --self-contained false -o "$publishBuildDirectory" /p:OutputPath="$TempDirectoryBuild"
    }

    Remove-Item $TempDirectoryBuild -Recurse -ErrorAction SilentlyContinue
	if ($RemoveExe) {
        Get-ChildItem $publishBuildDirectory -Include *.exe -Recurse | Remove-Item -Force -Recurse
	}
	
    Get-ChildItem $publishBuildDirectory -Include *.pdb -Recurse | Remove-Item -Force -Recurse
    Get-ChildItem $publishBuildDirectory -Include *.xml -Recurse | Remove-Item -Force -Recurse
}

function Get-Last-Version {
    
    Remove-Item $deltaDirectory -Recurse -ErrorAction SilentlyContinue
    New-Item $deltaDirectory -ItemType Directory -ErrorAction SilentlyContinue
    $arguments = "DownloadPackage --extract --outputpath `"$deltaDirectory`" --allowprereleases `"$IsPrerelease`" --metadatafilename `"$MetadataFileName`""
	
    if ($UseGitHubDelta) {
        $arguments += " --source GitHub --githubusername `"$GitHubUserName`" --githubrepositoryname `"$GitHubRepoName`" --githublegacyfallbackpattern `"$GitHubFallbackPattern`" --githubinheritversionfromtag `"$GitHubInheritVersionFromTag`""
    }
    elseif ($UseNuGetDelta) {
        $arguments += " --source NuGet --nugetpackageid `"$NuGetPackageId`" --nugetfeedurl `"$NuGetFeedUrl`" --nugetallowunlisted `"$NuGetAllowUnlisted`""
    }
    elseif ($UseGameBananaDelta) {
        $arguments += " --source GameBanana --gamebananaitemid `"$GameBananaItemId`""
    }

	Invoke-Expression "dotnet `"$updateToolPath`" $arguments"
}

function Get-Common-Publish-Args {
	
	param (
        $AllowDeltas=$True
    )
	
	$arguments = "--modfolder `"$publishBuildDirectory`" --packagename `"$PackageName`""
	if ($ChangelogPath) {
        $arguments += " --changelogpath `"$changelogFullPath`""
	}

    if ($ReadmePath) {
        $arguments += " --readmepath `"$readmeFullPath`""
	}
	
	if ($AllowDeltas -and $MakeDelta) {
        $arguments += " --olderversionfolders `"$deltaDirectory`""
	}
	
	return $arguments
}

function Publish-Common {

	param (
        $Directory="",
        $AllowDeltas=$True,
        $PublishTarget=""
    )
    
    Remove-Item $Directory -Recurse -ErrorAction SilentlyContinue
    New-Item $Directory -ItemType Directory -ErrorAction SilentlyContinue
	$arguments = "$(Get-Common-Publish-Args -AllowDeltas $AllowDeltas) --outputfolder `"$Directory`" --publishtarget $PublishTarget"
	$command = "$reloadedToolPath $arguments"
	Write-Host "$command`r`n`r`n"
	Invoke-Expression $command
}

function Publish-GameBanana {
    Publish-Common -Directory $PublishGameBananaDirectory -PublishTarget GameBanana
}

function Publish-NuGet {
    Publish-Common -Directory $PublishNuGetDirectory -PublishTarget NuGet -AllowDeltas $False
}

function Publish-Generic {
    Publish-Common -Directory $PublishGenericDirectory -PublishTarget Default
}

function Cleanup {
    Remove-Item $PublishOutputDir -Recurse -ErrorAction SilentlyContinue
    Remove-Item $PublishNuGetDirectory -Recurse -ErrorAction SilentlyContinue
    Remove-Item $PublishGenericDirectory -Recurse -ErrorAction SilentlyContinue
    Remove-Item $publishBuildDirectory -Recurse -ErrorAction SilentlyContinue
    Remove-Item $deltaDirectory -Recurse -ErrorAction SilentlyContinue
}

# Build & Publish
New-Item $TempDirectory -ItemType Directory -ErrorAction SilentlyContinue
Cleanup
Get-Tools

if ($MakeDelta) {
    Write-Host "Downloading Delta (Last Version)"
    Get-Last-Version
}

if ($Build) {
    Write-Host "Building Mod"
    Build    
}

if ($PublishGeneric) {
    Write-Host "Publishing Mod for Default Target"
    Publish-Generic
}

if ($PublishNuGet) {
    Write-Host "Publishing Mod for NuGet Target"
    Publish-NuGet
}

if ($PublishGameBanana) {
    Write-Host "Publishing Mod for GameBanana Target"
    Publish-GameBanana
}

Remove-Item $TempDirectory -Recurse -ErrorAction SilentlyContinue

Write-Host "Done."
Write-Host "Upload the files in folder `"$PublishOutputDir`" to respective location or website."
if ($UseScriptDirectory) {
    Pop-Location
}