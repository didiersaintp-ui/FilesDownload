using Azure.Identity;
using DeviceManifest.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Azure credentials (Managed Identity in production, DefaultAzureCredential for dev)
var credential = new DefaultAzureCredential();

// Register BlobStorageService
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var storageAccountName = configuration["Azure:StorageAccountName"] ?? throw new InvalidOperationException("Azure:StorageAccountName not configured");
    var containerName = configuration["Azure:ContainerName"] ?? "device-files";

    return new BlobStorageService(storageAccountName, containerName, credential);
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
