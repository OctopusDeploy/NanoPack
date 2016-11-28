$vhd = '#{vhd}'
$inputFolder = '#{inputFolder}'
$publishFolder = '#{publishFolder}'
$machineName = '#{machineName}'
$nanoserverFolder = '#{nanoServerFolder}'
$edition = '#{edition}'
$password='#{vmpassword}'
$firstBootScripts = '#{firstBootScripts}'
$additional = '#{additional}'
$maxSize = '#{maxSize}'
$copyPath = '#{copyPath}'
$ErrorActionPreference = "Stop"


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
$command = "New-NanoServerImage "
$command += "-MediaPath $nanoserverFolder " 
$command += "-Edition $edition " 
$command += "-DeploymentType Guest " 
$command += "-TargetPath $vhd "
$command += "-AdministratorPassword (ConvertTo-Securestring -asplaintext -force $password) "
$command += "-EnableRemoteManagementPort "
$command += "-ComputerName $machineName "
$command += "-SetupCompleteCommand ('PowerShell.exe -NoProfile -NonInteractive -NoLogo -ExecutionPolicy Unrestricted -Command ""& { %SystemDrive%\first-boot.ps1 }""') "
$command += "-LogPath "".\logs"" "
$command += "-MaxSize $maxSize "
If($copyPath)
{
	#eval copy path as it needs to be a powershell array or hashmap
	$command += "-CopyPath (Invoke-Expression $copyPath) "
}
$command += $additional

Write-NanoLog ("Command is {0}" -f ($command -replace $password, "*********"))
Invoke-Expression $command

Write-NanoLog "Mounting VHD"
$mountPath = ".\mount"
New-Item -ItemType directory -Force -Path $mountPath 
Mount-WindowsImage -ImagePath $vhd -Path $mountPath -Index 1

Try
{
	Write-NanoLog "Installing IIS to VHD"
    Add-WindowsPackage -Path $mountPath -PackagePath $nanoserverFolder\NanoServer\Packages\Microsoft-NanoServer-IIS-Package.cab

	#Don't use the New-NanoServerImage -CopyPath command so that users can override it
	Write-NanoLog "Copying additional AspNetCore files"
	Copy-Item -Path .\aspnetcore.dll -Destination $mountPath\windows\system32\inetsrv
    Copy-Item -Path .\aspnetcore_schema.xml -Destination $mountPath\windows\system32\inetsrv\config\schema

	Write-NanoLog "Copying main first-boot script"
	Copy-Item -Path .\first-boot.ps1 -Destination $mountPath

	Write-NanoLog "Copying application files to VHD"
	$appPath = Join-Path -Path $mountPath -ChildPath $publishFolder
	# copy with robocopy to avoid path length issues since we are mounting the vhd deep into an existing folder structure
	robocopy $inputFolder $appPath /E

	New-Item -ItemType directory -Path $mountPath\FirstBootScripts
	$firstBootScripts.Split(";", [System.StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object {
		Write-NanoLog "Adding $_ to VHD first-boot scripts"
		Copy-Item -Path $_ -Destination $mountPath\FirstBootScripts
	}

	Write-NanoLog "Dismounting VHD"
	Dismount-WindowsImage -Path $mountPath -Save
}
Catch
{
	Write-NanoLog "An error has occured during VHD creation. VHD dismounted without saving."
    Dismount-WindowsImage -Path $mountPath -Discard
	throw
}

Write-NanoLog "Making VHD writable"
#add permissions to the vhd
$acl = Get-Acl $vhd
$acl.SetAccessRuleProtection($True, $False)
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("NT AUTHORITY\Authenticated Users","FullControl", "Allow")
$acl.AddAccessRule($rule)
Set-Acl $vhd $acl

exit 0