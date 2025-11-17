using DeviceManifest.Api.Minio.Services;
using DeviceManifest.Shared;
using Minio;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MinIO client
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var endpoint = configuration["MinIO:Endpoint"] ?? throw new InvalidOperationException("MinIO:Endpoint not configured");
    var accessKey = configuration["MinIO:AccessKey"] ?? throw new InvalidOperationException("MinIO:AccessKey not configured");
    var secretKey = configuration["MinIO:SecretKey"] ?? throw new InvalidOperationException("MinIO:SecretKey not configured");
    var useSsl = bool.Parse(configuration["MinIO:UseSSL"] ?? "false");

    var minioClient = new MinioClient()
        .WithEndpoint(endpoint)
        .WithCredentials(accessKey, secretKey);

    if (useSsl)
    {
        minioClient = minioClient.WithSSL();
    }

    return minioClient.Build();
});

// Register MinioStorageService as IStorageService
builder.Services.AddSingleton<IStorageService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var minioClient = sp.GetRequiredService<IMinioClient>();
    var bucketName = configuration["MinIO:BucketName"] ?? "device-files";

    return new MinioStorageService(minioClient, bucketName);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
