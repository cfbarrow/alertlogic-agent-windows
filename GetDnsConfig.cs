using System.Collections.Generic;
using Heijden.DNS;
using ftalertlogicagent;

namespace GetDnsConfig
{
	public class DnsTest
	{
		private readonly Resolver _resolver;

		public DnsTest()
		{
			_resolver = new Resolver();
			_resolver.Recursion = true;
			_resolver.UseCache = true;
			_resolver.DnsServer = Deploy.get_dns_server(); // call public static string

			_resolver.TimeOut = 1000;
			_resolver.Retries = 3;
			_resolver.TransportType = TransportType.Udp;
		}
		// Get TXT record for DNS
		public IList<string> TxtRecords(string name)
		{
			IList<string> records = new List<string>();
			const QType qType = QType.TXT;
			const QClass qClass = QClass.IN;

			Response response = _resolver.Query(name, qType, qClass);

			foreach (RecordTXT record in response.RecordsTXT)
			{
				records.Add(record.ToString());
			}

			return records;
		}
	}
}
