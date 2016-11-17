$ErrorActionPreference="SilentlyContinue"
Stop-Transcript | out-null
$ErrorActionPreference = "Continue"
Start-Transcript -path C:\first-boot-script-output.log -append

$appname = '#{appname}'
$bindingInfo = "*:#{port}:"

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
New-IISSite -Name "AspNetCore" -PhysicalPath $env:SystemDrive\PublishedApps\$appname -BindingInformation $bindingInfo

Stop-Transcript