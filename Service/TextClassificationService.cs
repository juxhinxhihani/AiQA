using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Amazon.Bedrock;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Auth;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using QAAI.Model;

namespace QAAI.Service;
public class TextClassificationService
{
    private readonly S3Service _s3Service;
    private readonly AmazonBedrockRuntimeClient _bedrockClient;
    private const string modelId = "meta.llama3-1-70b-instruct-v1:0"; // Updated model ID
    private readonly IAmazonSimpleEmailService _simpleEmailService;

    public TextClassificationService(AmazonBedrockRuntimeClient bedrockClient, S3Service s3Service, IAmazonSimpleEmailService simpleEmailService)
    {
        _bedrockClient = bedrockClient;
        _s3Service = s3Service;
        _simpleEmailService = simpleEmailService;
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
        ModelResponse responseFromGenAi = new ModelResponse();

        string s3Data =  await _s3Service.GetDataAsync(); 

        var formattedInput = $"<|begin_of_text|><|start_header_id|>user<|end_header_id|>Based on this data {s3Data} answer to this complain :{inputText}, with only the name of category without explanations.<|eot_id|>\n<|start_header_id|>assistant<|end_header_id|>\n";

        var requestBody = JsonConvert.SerializeObject(new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 200,
            top_k = 250,
            stop_sequences = new string[] { },
            temperature = 0.5,
            top_p = 0.5,
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

                var emailResponse = await SendEmailAsync(new List<string> { "aldi.gjoka@raiffeisen.al" }, inputText, modelResponse.Content.FirstOrDefault().Text, "jon.hoxha@raiffeisen.al");

                responseFromGenAi.Generation = modelResponse.Content.FirstOrDefault().Text;

                var responseForClient = await GetCorrectResponse(inputText);

                responseFromGenAi.ClientResponse = responseForClient.ClientResponse;


                return responseFromGenAi;
                //return new ModelResponse()
                //{
                //    Generation = modelResponse.Content.FirstOrDefault().Text,
                //    GenerationTokenCount = modelResponse.Usage.OutputTokens,
                //    PromptTokenCount = modelResponse.Usage.InputTokens,
                //    StopReason = modelResponse.StopReason

                //};

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


    public async Task<string> SendEmailAsync(List<string> toAddresses,

        string bodyText, string subject, string senderAddress)
    {
        var messageId = "";
        try
        {
            var response = await _simpleEmailService.SendEmailAsync(
                new SendEmailRequest
                {
                    Destination = new Destination
                    {
                        ToAddresses = toAddresses
                    },
                    Message = new Amazon.SimpleEmail.Model.Message
                    {
                        Body = new Body
                        {
                            Html = new Content
                            {
                                Charset = "UTF-8",
                                Data = $"Pershendetje \n Ka ardhur kjo ankese: {bodyText} \n Faleminderit"
                            },
                            Text = new Content
                            {
                                Charset = "UTF-8",
                                Data = $"Pershendetje \n Ka ardhur kjo ankese: {bodyText} \n Faleminderit"
                            }
                        },
                        Subject = new Content
                        {
                            Charset = "UTF-8",
                            Data = subject
                        }
                    },
                    Source = senderAddress
                });
            messageId = response.MessageId;
        }
        catch (Exception ex)
        {
            Console.WriteLine("SendEmailAsync failed with exception: " + ex.Message);
        }

        return messageId;
    }
    public async Task<ModelResponse> GetCorrectResponse(string inputBody)
    {
        var formattedInput = $"<|begin_of_text|><|start_header_id|>user<|end_header_id|>Return a short response to this customer in a friendly way to let the customer know that he is compliant is beeing followed up: {inputBody}<|eot_id|>\n<|start_header_id|>assistant<|end_header_id|>\n";

        var requestBody = JsonConvert.SerializeObject(new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 200,
            top_k = 250,
            stop_sequences = new string[] { },
            temperature = 0.5,
            top_p = 0.5,
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
                    ClientResponse = modelResponse.Content.FirstOrDefault().Text,
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