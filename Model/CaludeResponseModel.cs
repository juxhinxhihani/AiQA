using Newtonsoft.Json;

namespace QAAI.Model;

public class CaludeResponseModel
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("role")]
    public string Role { get; set; }

    [JsonProperty("model")]
    public string Model { get; set; }

    [JsonProperty("content")]
    public List<ContentItem> Content { get; set; }

    [JsonProperty("stop_reason")]
    public string StopReason { get; set; }

    [JsonProperty("stop_sequence")]
    public object StopSequence { get; set; }

    [JsonProperty("usage")]
    public Usage Usage { get; set; }
}

public class ContentItem
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("text")]
    public string Text { get; set; }
}

public class Usage
{
    [JsonProperty("input_tokens")]
    public int InputTokens { get; set; }

    [JsonProperty("output_tokens")]
    public int OutputTokens { get; set; }
}


