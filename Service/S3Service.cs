using Amazon.S3;
using Amazon.S3.Model;

namespace QAAI.Service;

public class S3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName = "vocqaaitraindata"; 

    public S3Service(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }

    public async Task<string> GetDataAsync()
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = "Complaints.json"
        };

        using (var response = await _s3Client.GetObjectAsync(request))
        using (var reader = new StreamReader(response.ResponseStream))
        {
            string data = await reader.ReadToEndAsync();
            return data;
        }
    }
}