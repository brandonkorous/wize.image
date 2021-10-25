using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading.Tasks;
using wize.common.tenancy.Interfaces;
using wize.image.data.V1;
using wize.image.data.V1.Models;
using wize.image.odata.V1.Models;

namespace wize.image.odata.V1.Controllers
{
    [ApiVersion("1.0")]
    [ODataRoutePrefix("Image")]
    public class ImageController : ODataController
    {
        private readonly ImageContext _context;
        private readonly ILogger<ImagesController> _logger;
        private readonly IConfiguration _config;
        private readonly ITenantProvider _tenantProvider;

        public ImageController(ImageContext context, ILogger<ImagesController> logger, IConfiguration config, ITenantProvider tenantProvider)
        {
            _context = context;
            _logger = logger;
            _config = config;
            _tenantProvider = tenantProvider;
        }

        //[Authorize]
        [HttpGet]
        [EnableQuery(AllowedFunctions = AllowedFunctions.None, AllowedQueryOptions = AllowedQueryOptions.None)]
        public virtual async Task<Microsoft.AspNetCore.Mvc.FileContentResult> GetImage([FromODataUri] Guid imageId)
        {

            var image = _context.Images.Find(imageId);

            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(_config.GetValue<string>("ConnectionStrings_AzureBlobStorage"));
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            string strContainerName = "uploads";
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(strContainerName);
            var blob = cloudBlobContainer.GetBlobReference(image.Name);
            using (MemoryStream ms = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(ms);
                return File(ms.ToArray(), image.MIME);
            }

            return null;
        }

        [HttpGet]
        [EnableQuery(AllowedFunctions = AllowedFunctions.None, AllowedQueryOptions = AllowedQueryOptions.None)]
        public virtual async Task<Microsoft.AspNetCore.Mvc.FileContentResult> GetSizedImage([FromODataUri] Guid imageId, [FromODataUri] int height, [FromODataUri] int width)
        {

            var image = _context.Images.Find(imageId);

            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(_config.GetValue<string>("ConnectionStrings_AzureBlobStorage"));
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            string strContainerName = "uploads";
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(strContainerName);
            var blob = cloudBlobContainer.GetBlobReference(image.Name);
            using (MemoryStream ms = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(ms);
                var fullImage = SixLabors.ImageSharp.Image.Load(ms.ToArray());
                ResizeOptions options = new ResizeOptions() { Size = new SixLabors.ImageSharp.Size(height, width), Mode = ResizeMode.Max };
                fullImage.Mutate(x => x.Resize(options).BackgroundColor(SixLabors.ImageSharp.Color.Transparent));
                using (MemoryStream outStream = new MemoryStream())
                {
                    fullImage.Save(outStream, new PngEncoder());
                    return File(outStream.ToArray(), "image/png");
                }
            }

            return null;
        }

        [HttpPost]
        //[ODataRoute("Something")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[EnableQuery(AllowedFunctions = AllowedFunctions.None, AllowedQueryOptions = AllowedQueryOptions.None)]
        public virtual async Task<IActionResult> Something([FromBody] SomethingModel model)
        {
            return Ok(model);
        }

        [Authorize("update:image")]
        [HttpPost]
        //[ODataRoute("UploadImage")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [EnableQuery(AllowedFunctions = AllowedFunctions.None, AllowedQueryOptions = AllowedQueryOptions.None)]
        public virtual async Task<IActionResult> UploadImage(ODataActionParameters parameters)//[odata] ImageDTO model)
        {
            try
            {
                if(parameters == null || !parameters.ContainsKey("model"))
                {
                    return BadRequest();
                }
                var model = (ImageDTO)parameters["model"];
                TryValidateModel(model);

                if(!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(_config.GetValue<string>("ConnectionStrings_AzureBlobStorage"));
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                string strContainerName = "uploads";
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(strContainerName);

                if (await cloudBlobContainer.CreateIfNotExistsAsync())
                {
                    await cloudBlobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                }

                if (model.OriginalName != null && model.Blob != null)
                {
                    model.Name = $"{Path.GetFileNameWithoutExtension(model.OriginalName)}-{DateTime.Now.ToFileTimeUtc()}{Path.GetExtension(model.OriginalName)}";
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(model.Name);
                    cloudBlockBlob.Properties.ContentType = model.MIME;
                    await cloudBlockBlob.UploadFromByteArrayAsync(model.Blob, 0, model.Blob.Length);
                    model.Url = cloudBlockBlob.Uri.AbsoluteUri;
                    var image = new Image
                    {
                        Created = model.Created,
                        CreatedBy = model.CreatedBy,
                        MIME = model.MIME,
                        Name = model.Name,
                        OriginalName = model.OriginalName,
                        Published = model.Published,
                        Url = model.Url
                    };

                    _context.Images.Add(image);
                    _context.SaveChanges();

                    return Ok(image.ImageId);
                }
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error: UploadImage():{0}", typeof(Image).Name);
                return new StatusCodeResult(500);
            }
        }
    }
}
