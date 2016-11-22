$appName = '#{appName}'
$vhd = '#{vhd}'
$inputFolder = '#{inputFolder}'
$publishFolder = '#{publishFolder}'
$machineName = '#{machineName}'
$nanoserverFolder = '#{nanoServerFolder}'
$edition = '#{edition}'
$Password='#{vmpassword}'
$ErrorActionPreference = "Stop"
$SecurePassword=(ConvertTo-Securestring -asplaintext -force $Password)

Function Write-NanoLog {
    Param ([string]$message)
    Write-Host NanoPack: $message
}

If (Test-Path $vhd){ 
	Write-NanoLog "Removing existing VHD at $vhd"
	Remove-Item $vhd
}

Write-NanoLog "Importing NanoServerImageGenerator"
Import-Module -Name $nanoserverFolder\Nanoserver\NanoServerImageGenerator\NanoServerImageGenerator

Write-NanoLog "Creating VHD at $vhd, this may take a while"
New-NanoServerImage `
    -MediaPath $nanoserverFolder `
    -Edition $edition `
    -DeploymentType Guest `
    -TargetPath $vhd `
    -AdministratorPassword $SecurePassword `
    -EnableRemoteManagementPort `
    -ComputerName $machineName `
    -SetupCompleteCommand ('PowerShell.exe -NoProfile -NonInteractive -NoLogo -ExecutionPolicy Unrestricted -Command "& { %SystemDrive%\first-boot.ps1 }"') `
    -CopyPath (".\first-boot.ps1") `
    -LogPath ".\logs"

Write-NanoLog "Mounting VHD"
$mountPath = ".\mount"
New-Item -ItemType directory -Force -Path $mountPath 
Mount-WindowsImage -ImagePath $vhd -Path $mountPath -Index 1

Try
{
	Write-NanoLog "Installing IIS to VHD"
    Add-WindowsPackage -Path $mountPath -PackagePath $nanoserverFolder\NanoServer\Packages\Microsoft-NanoServer-IIS-Package.cab

	Write-NanoLog "Copying additional AspNetCore files"
	Copy-Item -Path .\aspnetcore.dll -Destination $mountPath\windows\system32\inetsrv
    Copy-Item -Path .\aspnetcore_schema.xml -Destination $mountPath\windows\system32\inetsrv\config\schema

	Write-NanoLog "Copying application files to VHD"
	$appPath = Join-Path -Path $mountPath -ChildPath $publishFolder
    New-Item -ItemType Directory -Force -Path $appPath
    Copy-Item -Path $inputFolder\* -Destination $appPath -Recurse

	Write-NanoLog "Dismounting VHD"
	Dismount-WindowsImage -Path $mountPath -Save
}
Catch
{
	Write-NanoLog "An error has occured during VHD creation. VHD dismounted without saving."
    Dismount-WindowsImage -Path $mountPath
	throw
}

Write-NanoLog "Making VHD writable"
#add permissions to the vhd
$acl = Get-Acl $vhd
$acl.SetAccessRuleProtection($True, $False)
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("NT AUTHORITY\Authenticated Users","FullControl", "Allow")
$acl.AddAccessRule($rule)
Set-Acl $vhd $acl
