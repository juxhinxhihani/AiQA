using Newtonsoft.Json;

namespace QAAI.Model;

public class ModelResponse
{
    [JsonProperty("generation")]
    public string Generation { get; set; }

    [JsonProperty("ClientResponse")]
    public string ClientResponse { get; set; }

    [JsonProperty("prompt_token_count")]
    public int PromptTokenCount { get; set; }

    [JsonProperty("generation_token_count")]
    public int GenerationTokenCount { get; set; }

    [JsonProperty("stop_reason")]
    public string StopReason { get; set; }
}