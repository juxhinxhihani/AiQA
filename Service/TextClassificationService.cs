using System.Text;
using System.Text.Json.Nodes;
using Amazon.Bedrock;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Auth;
using Newtonsoft.Json;
using QAAI.Model;

namespace QAAI.Service;
public class TextClassificationService
{
    private readonly S3Service _s3Service;
    private readonly AmazonBedrockRuntimeClient _bedrockClient;
    private const string modelId = "meta.llama3-1-70b-instruct-v1:0"; // Updated model ID

    public TextClassificationService(AmazonBedrockRuntimeClient bedrockClient, S3Service s3Service)
    {
        _bedrockClient = bedrockClient;
        _s3Service = s3Service;
    }

    public async Task<ModelResponse> ClassifyTextMetaAsync(string inputText)
    {
        
        string s3Data =  await _s3Service.GetDataAsync(); 

        var formattedInput = $"<|begin_of_text|><|start_header_id|>user<|end_header_id|>{s3Data}Te lutem pergjigju kesaj:{inputText}, ne baze te kategorise?<|eot_id|>\n<|start_header_id|>assistant<|end_header_id|>\n";

        var nativeRequest = JsonConvert.SerializeObject(new
        {
            prompt = formattedInput,
            max_gen_len = 50,
            temperature = 0.5
        });
        var request = new InvokeModelRequest()
        {
            ModelId = modelId,
            Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(nativeRequest)),
            ContentType = "application/json"
        };
        
        try
        {
            var response = await _bedrockClient.InvokeModelAsync(request);
            using (var reader = new StreamReader(response.Body))
            {
                string responseBody = await reader.ReadToEndAsync();
                ModelResponse modelResponse = JsonConvert.DeserializeObject<ModelResponse>(responseBody);
                return modelResponse;

            }
        }
        catch (AmazonBedrockRuntimeException ex)
        {
            // Handle Bedrock runtime exceptions
            throw new ApplicationException($"Bedrock runtime error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            // Handle other exceptions
            throw new ApplicationException($"An error occurred: {ex.Message}", ex);
        }
    }
    
    public async Task<ModelResponse> ClassifyTextClaudeAsync(string inputText)
    {
        
        string s3Data =  await _s3Service.GetDataAsync(); 

        var formattedInput = $"<|begin_of_text|><|start_header_id|>user<|end_header_id|>Based on this data {s3Data} answer to this complain :{inputText}, with only the name of category without explanations.<|eot_id|>\n<|start_header_id|>assistant<|end_header_id|>\n";

        var requestBody = JsonConvert.SerializeObject(new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 200,
            top_k = 250,
            stop_sequences = new string[] { },
            temperature = 1,
            top_p = 0.999,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = formattedInput
                        }
                    }
                }
            }
        });

        // Create the InvokeModelRequest
        var request = new InvokeModelRequest
        {
            ModelId = "anthropic.claude-3-5-sonnet-20241022-v2:0",
            ContentType = "application/json",
            Accept = "application/json",
            Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(requestBody)),
        };
        
        try
        {
            var response = await _bedrockClient.InvokeModelAsync(request);
            using (var reader = new StreamReader(response.Body))
            {
                string responseBody = await reader.ReadToEndAsync();
                CaludeResponseModel modelResponse = JsonConvert.DeserializeObject<CaludeResponseModel>(responseBody);
                return new ModelResponse()
                {
                    Generation = modelResponse.Content.FirstOrDefault().Text,
                    GenerationTokenCount = modelResponse.Usage.OutputTokens,
                    PromptTokenCount = modelResponse.Usage.InputTokens,
                    StopReason = modelResponse.StopReason
                    
                };

            }
        }
        catch (AmazonBedrockRuntimeException ex)
        {
            // Handle Bedrock runtime exceptions
            throw new ApplicationException($"Bedrock runtime error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            // Handle other exceptions
            throw new ApplicationException($"An error occurred: {ex.Message}", ex);
        }
    }
}