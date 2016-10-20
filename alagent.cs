using System;
using System.Net;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft;
using Microsoft.CSharp;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using GetDnsConfig;
using System.Net.NetworkInformation;
using System.Threading;
using System.Reflection;


namespace ftalertlogicagent
{

	public class Deploy
	{
		
		static string URL = "http://169.254.169.254/latest/dynamic/instance-identity/document";
		static string AL_SUFFIX = "alertlogic.in.ft.com";
		static string HOST = get_hostname();
		static string DIR = get_wd();
		static string MSI = "al_agent.msi";
		static string AL_CERT_PATH = get_install_path()+"\\AlertLogic\\agent\\ca_crt.pem";
		static string AL_INST_MSI = DIR+"\\"+MSI;
		static string SOURCE_TYPE = "alertlogic_deployment";
		static string LEG_URL = "legacy.alertlogic.in.ft.com";

		private EventLog _Logger;

		public void EvtLog() {
			_Logger = new EventLog("Application");
			_Logger.Source = SOURCE_TYPE;

		}

		// main function
		public static void Alagent ()
		{
			Deploy Deploy = new Deploy();

			Deploy.EvtLog ();
			Deploy.Start();
			// delete installation file
			Deploy.file_delete (AL_INST_MSI);
		}

		public static string get_wd () {
			return System.IO.Directory.GetCurrentDirectory();
		}

		public static string get_hostname () {
			return System.Environment.MachineName;
		}

		public static string get_install_path () {
			return Environment.GetEnvironmentVariable ("programfiles(x86)");
		}

		public void file_delete(string file_path){
			if (File.Exists(file_path)){
				File.Delete (file_path);
			}
		}


		private static string get_info_aws(string input_url)
		{

			string respStr = "";
			var httpWebRequest = (HttpWebRequest)WebRequest.Create(input_url);
			httpWebRequest.ContentType = "application/json";
			httpWebRequest.Accept = "*/*";
			httpWebRequest.Method = "GET";
			httpWebRequest.Timeout = 5000;

			try
			{
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				respStr = new System.IO.StreamReader(httpResponse.GetResponseStream()).ReadToEnd();
			}
			catch (WebException e)
			{
				if (e.Status == WebExceptionStatus.Timeout)
				{
					string mess = "Request timeout on "+input_url;
					Console.WriteLine(mess);
				}
			}
			return respStr;				

		}

		// Return DNS Server
		public static string get_dns_server()
		{
			NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface networkInterface in networkInterfaces)
			{
				if (networkInterface.OperationalStatus == OperationalStatus.Up)
				{
					IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
					IPAddressCollection dnsAddresses = ipProperties.DnsAddresses;

					foreach (IPAddress dnsAdress in dnsAddresses)
					{
						return dnsAdress.ToString();
					}
				}
			}

			throw new InvalidOperationException("Unable to find DNS  Address");
		}

		// Last chars avaibility zone
		private static char get_avaibility_zone(JObject aws_json){
			string str_out = aws_json["availabilityZone"].ToString();
			char last_char_avaib_zone = str_out[str_out.Length - 1];
			return last_char_avaib_zone;	
		}

		// get account id
		private static string get_account_id(JObject aws_json){
			string str_out = aws_json ["accountId"].ToString ();
			return str_out;
		}

		// get region
		private static string get_region(JObject aws_json){
			string str_out = aws_json ["region"].ToString ();
			return str_out;
		}

		// Extract file from exe to directory
		public static void WriteResourceToFile(string resourceName, string fileName)
		{
			using(var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
			{
				using(var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
				{
					resource.CopyTo(file);
				} 
			}
		}


		// Get Al Provision key
		private string get_provkey(string input_url) {
			string txt_record = "";
			DnsTest dnsTest = new DnsTest();
			IList<string> txt_record_list = dnsTest.TxtRecords(input_url);
			txt_record = string.Join(",", txt_record_list.ToArray());

			return txt_record;
		}

		// Check syntax provision key
		private bool check_prov_key(string input_key) {
			Regex regex = new Regex (@"^[a-zA-Z0-9]+$");
			Match match = regex.Match (input_key);
			if (! match.Success) {
				Console.WriteLine ("Invalid provision key " + input_key);
				return false;
			}
			return true;
		}

		// start CMD
		private static int start_cmd(string cmd, string args){
			System.Diagnostics.Process installerProcess  = new System.Diagnostics.Process();
			installerProcess.StartInfo.FileName = cmd;
			installerProcess.StartInfo.Arguments = args;
			installerProcess.StartInfo.UseShellExecute = false;
			installerProcess.Start();
			while (installerProcess.HasExited==false)
			{
				//indicate progress to user
				//Application.DoEvents();
				System.Threading.Thread.Sleep(100);
			}
			int exitcode = installerProcess.ExitCode;
			return exitcode;
		}


		// Deploy agent function
		private void Start()
		{

			string alertlogic_get_provkey = "";
			string respStr = "";
			string alertlogic_url = "";


			// get json string
			respStr = get_info_aws (URL);

			// Case AWS
			if (respStr != "") {
				JObject awsJson = JObject.Parse (respStr);

				char last_char_avaib_zone = get_avaibility_zone(awsJson);
				string account_id = get_account_id (awsJson);
				string region = get_region (awsJson);

				// URL for provision host
				alertlogic_url = last_char_avaib_zone + "." + region +"." + account_id +"."+  AL_SUFFIX;
				alertlogic_get_provkey = account_id +"."+ AL_SUFFIX;

				// UCS
			} else { 

				Regex regex = new Regex (@"((^ft[a-z]{3}[0-9]+-wv([a-z]{2})))");
				Match match = regex.Match (HOST.ToLower());
				if (match.Success) {
					string match_sucess = match.Value;
					string area = match_sucess.Substring (match_sucess.Length - 2);
					// URL for provision host
					alertlogic_url = area + "." + AL_SUFFIX;
					alertlogic_get_provkey = "ucs."+AL_SUFFIX;
				} else {
					string mess = "Using the default provisioning host" + LEG_URL;
					Console.WriteLine (mess);
					_Logger.WriteEntry (mess);
					alertlogic_url = LEG_URL;
					alertlogic_get_provkey = "_alprovkey."+LEG_URL;
				}
			}

			// Get provision key
			string provkey = get_provkey("_alprovkey."+alertlogic_get_provkey);

			if (check_prov_key(provkey)) {
				Console.WriteLine ("Alertlogic Provision key: "+provkey+" - URL: "+ alertlogic_url);

				// extract al_agent.msi  from exe
				WriteResourceToFile ("ftalertlogicagent.Resources."+MSI,AL_INST_MSI);

				// Start installation
				string al_args = string.Format("/q /i {0} prov_key={1} sensor_host={2} install_only=1",MSI, provkey,alertlogic_url);
				string al_args_without_prov_key = string.Format("/q /i {0} sensor_host={1} install_only=1",MSI, alertlogic_url);

				string al_start_cmd = "/c net start al_agent";
				string al_start_auto_cmd = "/c sc config al_agent start= auto";

				// execute al_agent.msi
				int exitcode = start_cmd("msiexec",al_args);
				if (exitcode != 0 ) {
					string mess = "Something went wrong during the installation process, trying without provision key";
					Console.WriteLine (mess);
					_Logger.WriteEntry (mess);
					exitcode = start_cmd("msiexec",al_args_without_prov_key);
					if (exitcode != 0) {
						mess = "Unable to install Alertlogic agent"; 
						Console.WriteLine (mess);
						_Logger.WriteEntry (mess);
					}
				}


				int count = 0;
				do {
					// start al_agent
					start_cmd("cmd.exe",al_start_cmd);

					// configure start auto
					start_cmd("cmd.exe",al_start_auto_cmd);

					Thread.Sleep(3000);
					count++;
				} while (count < 3 & !File.Exists (AL_CERT_PATH));

				if (File.Exists (AL_CERT_PATH)) {
					string mess = "Alertlogic agent has been properly started";
					Console.WriteLine (mess);
					_Logger.WriteEntry (mess);
				} else {
					string mess = "Something went wront during the installation process";
					Console.WriteLine (mess);
					_Logger.WriteEntry (mess);
				}
			}

		}

	}
}
