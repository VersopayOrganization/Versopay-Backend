// Services/BlobStorageService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace VersopayBackend.Services
{
    public interface IBlobStorageService
    {
        (Uri sasUri, string blobName) GetUploadSas(string container, string blobName, TimeSpan ttl, BlobSasPermissions perms);
        Uri GetReadSas(string container, string blobName, TimeSpan ttl);
    }

    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _service;

        public BlobStorageService(BlobServiceClient service) => _service = service;

        public (Uri sasUri, string blobName) GetUploadSas(string container, string blobName, TimeSpan ttl, BlobSasPermissions perms)
        {
            var cont = _service.GetBlobContainerClient(container);
            cont.CreateIfNotExists(); // idempotente

            var blob = cont.GetBlobClient(blobName);
            var sas = new BlobSasBuilder(perms, DateTimeOffset.UtcNow.Add(ttl))
            {
                BlobContainerName = container,
                BlobName = blobName,
                Resource = "b"
            };
            var uri = blob.GenerateSasUri(sas); // requer Shared Key no client
            return (uri, blobName);
        }

        public Uri GetReadSas(string container, string blobName, TimeSpan ttl)
        {
            var cont = _service.GetBlobContainerClient(container);
            var blob = cont.GetBlobClient(blobName);
            var sas = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(ttl))
            {
                BlobContainerName = container,
                BlobName = blobName,
                Resource = "b"
            };
            return blob.GenerateSasUri(sas);
        }
    }
}
