using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Reflection;
using System.IO;


namespace ftalertlogicagent
{
	public class main
	{

		public static void Main()
		{
			if (IsAdmin ()) {  // check admin before starting
				
				List<string> DLL_NAMES = new List<string>() { "Heijden.Dns.dll","Newtonsoft.Json.dll","Newtonsoft.Json.FSharp.dll","FSharp.Core.dll","NodaTime.dll"};
				load_dll ("ftalertlogicagent.Resources",DLL_NAMES);
				Deploy.Alagent();
			}
			else {
				string mess = "Please run the script as Administrator";
				Console.WriteLine(mess);
			}

		}

		public static void load_dll(string NameSpace, List<string> dlls){
			foreach (string dll in dlls) {
				WriteResourceToFile(NameSpace + "." + dll,dll);	
			}
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

		private static bool IsAdmin(){

			bool isElevated;
			WindowsIdentity identity = WindowsIdentity.GetCurrent();
			WindowsPrincipal principal = new WindowsPrincipal(identity);
			isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

			return isElevated;
		}
	}
}

