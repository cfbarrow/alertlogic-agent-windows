ft-security-whitesource
=========================

This module include two lambda functions which will allow you to send some information into splunk and create and healthcheck.

healthcheck is available here: http://healthcheck.ft.com/service/a238343130f47e5b1615a56dc9c9af28

White Source vulnerability overview per systemCode available here: https://s3-eu-west-1.amazonaws.com/app.ft-security-incident-monitor-1k1tztdno1tmc/whitesource.html

As this runs in Lambda, it will be pushed into Splunk
	
    sourcetype="aws:cloudwatchlogs:vpcflow" source="*/aws/lambda/ft-security-whitesource*"
    
For more information Please visit the page: 

https://sites.google.com/a/ft.com/security/continuous-delivery-security/white-source-software