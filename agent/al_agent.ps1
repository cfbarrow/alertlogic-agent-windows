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
        #echo $_.Exception.Message
        echo "Can't retrieve informations about instance-identity document"
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
    $region = $ident_json | ConvertFrom-Json | select region

    $pre = $val_avaib.availabilityZone.Substring($val_avaib.availabilityZone.Length-1)
    $alertlogic_url= $pre+"."+$region.region+"."+$val_acc.accountId+".alertlogic.in.ft.com"
    $alertlogic_get_provkey= $val_acc.accountId+".alertlogic.in.ft.com"

} else {
    <# IP #>
    $alertlogic_get_provkey = "ucs.alertlogic.in.ft.com"
    $alertlogic_url = $alertlogic_get_provkey
}


# Get provision key
$data = Resolve-DnsName  _alprovkey.$alertlogic_get_provkey -Type TXT
$alprovkey = $data.strings

<# Check syntax of provision key  #>
if (![ValidatePattern('^[a-zA-Z0-9]+$')]$alprovkey) {
    echo "invalid alertlogic provision key"
    exit 1
}

echo "the provision key is: "$alprovkey"the sensor domain is: "$alertlogic_url

<# install Alertlogic Agent #>
(Start-Process -FilePath "msiexec.exe" -ArgumentList "/i al_agent.msi prov_key=$alprovkey sensor_host=$alertlogic_url install_only=1 /q" -Wait -Passthru).ExitCode

if ($? -eq $true) {
    $path="%CommonProgramFiles(x86)%\AlertLogic\host_key.pem"
    $count=0
    do {
        cmd /c sc config al_agent start= auto
        $count++
        sleep 5
    } while ((Test-Path($path)) -And ($count-le3))

    echo "Alertlogic agent successfully deployed"
} else {
    echo "Something went wrong during the installation process"
}

s