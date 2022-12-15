using System;
using System.Text.Json.Serialization;

namespace Kafka.OpenSearch.Consumer.Models;

public class WikimediatDetails
{
	public MetaDetails Meta { get; set; }
    public long Id { get; set; }
	public string Type { get; set; }
	public int Namespace { get; set; }
	public string Title { get; set; }
	public string Comment { get; set; }
	public long Timestamp { get; set; }
	public string User { get; set; }
	public bool Bot { get; set; }
	public bool Minor { get; set; }
	public bool Patrolled { get; set; }
	public UpdateDetails Length { get; set; }
	public UpdateDetails Revision { get; set; }
	public string Server_url { get; set; }
	public string Server_name { get; set; }
	public string Server_script_path { get; set; }
	public string Wiki { get; set; }
	public string Parsedcomment { get; set; }
}

public class MetaDetails
{
	public string Uri { get; set; }
	public string Request_id { get; set; }
	public string Id { get; set; }
	public DateTime Dt { get; set; }
	public string Domain { get; set; }
	public string Stream { get; set; }
	public string Topic { get; set; }
	public int Partition { get; set; }
	public long Offset { get; set; }
}

public class UpdateDetails
{
	public long Old { get; set; }
	public long New { get; set; }
}