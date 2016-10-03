<#
.SYNOPSIS
Alertlogic windows agent
Author: Clyde Fondop
Required Dependencies: al_agent.msi available here: https://docs.alertlogic.com/install/alert-logic-agent-windows.htm

Version: 1.1

.DESCRIPTION
Installation du package MSI Alertlogic Agent provided by Alerlogic.
Provision key and register domain will be dynamically updated by using DNS records.
This tool work on UCS and AWS Windows boxes.

.EXAMPLE
./al_agent.ps1

.NOTES
This tool will install alertlogic agent on your operation system.
https://docs.alertlogic.com/install/alert-logic-agent-windows.htm
https://docs.alertlogic.com/requirements/system-requirements.htm#reqsAgent

Supported windows:
Windows Server 2003 (with powershell)
Windows Server 2008
Windows Server 2012
Windows Vista
Windows 7
Windows 8

.LINK
Git repository:
#>


function check_is_aws () {

    try{
        $identitydoc = curl http://169.254.169.254/latest/dynamic/instance-identity/document
    }
    catch {
        echo $_.Exception.Message
    }
    return $identitydoc
}


$identitydoc = check_is_aws
 <# AWS or UCS if not AWS #>
if ($identitydoc) {
    <# get content of AWS instance identity #>
    $ident_json = "$identitydoc"
    $val_acc = $ident_json | ConvertFrom-Json | select accountId
    $val_avaib =  $ident_json | ConvertFrom-Json | select availabilityZone
    $accountid = $val_acc.accountId
    $region = $val_avaib.availabilityZone
    $alertlogic_url= $region+"."+$accountid+".alertlogic.in.ft.com"

     <# Provkey #>
    $data = Resolve-DnsName _alprovkey.$accountid.alertlogic.in.ft.com -Type TXT
    $alprovkey = $data.strings
} else {
    <# IP #>
    $alertlogic_url = "ucs.alertlogic.in.ft.com"

     <# Provkey #>
    $data = Resolve-DnsName  -Type TXT
    $alprovkey = $data.strings
}

<# Check syntax of provision key  #>
if (![ValidatePattern('^[a-zA-Z0-9]+$')]$alprovkey) {
    echo "invalid alertlogic provision key"
    exit 1
}

<# install Alertlogic Agent #>
(Start-Process -FilePath "msiexec.exe" -ArgumentList "/i al_agent.msi prov_key=$alprovkey sensor_host=$alertlogic_url install_only=1 /q" -Wait -Passthru).ExitCode

if ($? -eq $true) {
    cmd /c sc config al_agent start= auto
    echo "Alertlogic agent successfully deployed"
} else {
    echo "Something went wrong during the installation process"
}

