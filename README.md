alertlogicagent-windows
=======================

Alertlogic is a security compliance plateform which could be for many security purposes, including networking intrusion detection and prevention WAF. 
The aim of this module if to be able to install the agent which will  makes the communication and send the traffic to the Alertlogic Threat Manager. 
Once this tool installed on the targeted host we will be able to monitor each activities (log events and network traffic) of our host.
This module is the executable which will allows you to install Alertlogic agent on Windows AWS. It is using informations configured in your DNS records. 

#### System requierments

* Windows Server 2003, SP1
* Windows Server 2008
* Windows Server 2012
* Windows Vista
* Windows 7
* Windows 8
* Windows XP SP1

#### Components of this module

* **al_agent.msi**: This executable allows your to install the latest version of the Alertlogic Agent on Windows.
* **ftalertlogicagent.exe**: This tool (if properly configured) can be used in a automatic deployment in order to ensure that Alertlogic agent is installed everywhere.



#### Usage

```
variable AL_SUFFIX and LEG_URL needs to be set up according to your DNS configuration.
    
ftalertlogicagent.exe

```

For more information Please visit the page: 

* https://docs.alertlogic.com/requirements/system-requirements.htm#reqsAgent
* https://docs.alertlogic.com/install/alert-logic-agent-windows.htm