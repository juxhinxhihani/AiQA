using Amazon.BedrockRuntime;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleEmail;
using QAAI.Model;
using QAAI.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddSingleton<AmazonBedrockRuntimeClient>(sp =>
{
    var awsSettings = builder.Configuration.GetSection("AWS").Get<AwsSettings>();
    var credentials = new SessionAWSCredentials(awsSettings.AccessKey, awsSettings.SecretKey, awsSettings.SessionToken);

    var config = new AmazonBedrockRuntimeConfig
    {
        RegionEndpoint = Amazon.RegionEndpoint.USWest2 
    };

    return new AmazonBedrockRuntimeClient(credentials, config);
});
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var awsSettings = builder.Configuration.GetSection("AWS").Get<AwsSettings>();
    var credentials = new SessionAWSCredentials(awsSettings.AccessKey, awsSettings.SecretKey, awsSettings.SessionToken);    
    var config = new AmazonS3Config
    {
        RegionEndpoint = Amazon.RegionEndpoint.USWest2 
    };
    return new AmazonS3Client(credentials, config);
});

builder.Services.AddSingleton<IAmazonSimpleEmailService>(sp =>
{
    var awsSettings = builder.Configuration.GetSection("AWS").Get<AwsSettings>();
    var credentials = new SessionAWSCredentials(awsSettings.AccessKey, awsSettings.SecretKey, awsSettings.SessionToken);

    return new AmazonSimpleEmailServiceClient(credentials, Amazon.RegionEndpoint.USWest2);
});


builder.Services.AddTransient<S3Service>();
builder.Services.AddTransient<TextClassificationService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(options =>
    options.AllowAnyHeader()
        .AllowAnyOrigin()
        .AllowAnyMethod()
);

app.UseHttpsRedirection();

app.MapControllers();
app.Run();
