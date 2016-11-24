$ErrorActionPreference="SilentlyContinue"
Stop-Transcript | out-null
$ErrorActionPreference = "Continue"
Start-Transcript -path C:\first-boot-script-output.log -append

$bindingInfo = '*:#{port}:'
$publishFolder = '#{publishFolder}'

# Open port in firewall
New-NetFirewallRule -Name "AspNet5 IIS" -DisplayName "Allow HTTP on TCP/#{port}" -Protocol TCP -LocalPort #{port} -Action Allow -Enabled True

Import-Module IISAdministration

$aspNetCoreHandlerFilePath="$env:SystemDrive\windows\system32\inetsrv\aspnetcore.dll"
Reset-IISServerManager -confirm:$false
$sm = Get-IISServerManager
$appHostconfig = $sm.GetApplicationHostConfiguration()

# Add AppSettings section 
$appHostconfig.RootSectionGroup.Sections.Add("appSettings")

# Set Allow for handlers section
$appHostconfig.GetSection("system.webServer/handlers").OverrideMode="Allow"

# Add aspNetCore section to system.webServer
$sectionaspNetCore = $appHostConfig.RootSectionGroup.SectionGroups["system.webServer"].Sections.Add("aspNetCore")
$sectionaspNetCore.OverrideModeDefault = "Allow"

$sm.CommitChanges()

# Configure globalModule
Reset-IISServerManager -confirm:$false
$globalModules = Get-IISConfigSection "system.webServer/globalModules" | Get-IISConfigCollection
New-IISConfigCollectionElement $globalModules -ConfigAttribute @{"name"="AspNetCoreModule";"image"=$aspNetCoreHandlerFilePath}

# Configure module
$modules = Get-IISConfigSection "system.webServer/modules" | Get-IISConfigCollection
New-IISConfigCollectionElement $modules -ConfigAttribute @{"name"="AspNetCoreModule"}

# Create Site
$appPath = Join-Path -Path $env:SystemDrive -ChildPath $publishFolder
Write-Host "Creating new IIS site serving $appPath"
New-IISSite -Name "AspNetCore" -PhysicalPath $appPath -BindingInformation $bindingInfo

# Run other first boot scripts
Get-ChildItem $env:SystemDrive\FirstBootScripts -Filter *.ps1 | ForEach-Object {
	Write-Host "Running $_..."
  & $_.FullName
}

Stop-Transcript