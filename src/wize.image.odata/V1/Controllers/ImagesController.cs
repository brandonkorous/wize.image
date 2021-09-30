﻿using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using wize.image.data.V1;
using wize.image.data.V1.Models;
using wize.image.odata.V1.Models;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using wize.common.tenancy.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace wize.image.odata.V1.Controllers
{
    [ApiVersion("1.0")]
    [ODataRoutePrefix("Images")]
    public class ImagesController : ODataController
    {
        private readonly ImageContext _context;
        private readonly ILogger<ImagesController> _logger;
        private readonly IConfiguration _config;
        private readonly ITenantProvider _tenantProvider;

        public ImagesController(ImageContext context, ILogger<ImagesController> logger, IConfiguration config, ITenantProvider tenantProvider)
        {
            _context = context;
            _logger = logger;
            _config = config;
            _tenantProvider = tenantProvider;
        }

        /// <summary>
        /// OData based GET operation.
        /// This method will return the requested Dataset.
        /// </summary>
        /// <returns>IQueryable of requested type.</returns>
        /// 
        [Authorize]
        [ODataRoute]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        //[HttpGet]
        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All)]
        public virtual ActionResult<IQueryable<Image>> Get()
        {
            try
            {
                Guid? tenantId = _tenantProvider.GetTenantId();
                return Ok(_context.Set<Image>().Where(a => EF.Property<Guid>(a, "TenantId") == tenantId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error: Get():{0}", typeof(Image).Name);
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// OData based GET(id) operation.
        /// This method receives a key value and will return the respective record if it exists.
        /// </summary>
        /// <param name="id">Key value</param>
        /// <returns>Data model</returns>
        [ODataRoute("({id})")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public virtual IActionResult Get(Guid id)
        {
            try
            {
                //_context.Set<TModel>().Single(m => m.)
                var model = _context.Find<Image>(id);

                if (model == null)
                {
                    _logger.LogWarning("Warning: Get(id):{0} NotFound", typeof(Image).Name);
                    return NotFound();
                }
                return Ok(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error: Get {0}", typeof(Image).Name);
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// OData based POST operation.
        /// This method receives a model and attempts to insert that record into the appropriate datastore.
        /// </summary>
        /// <param name="model">Data model</param>
        /// <returns>Data model</returns>
        [Authorize]
        [ODataRoute]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public virtual IActionResult Post([FromBody] Image model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                _context.Set<Image>().Add(model);
                _context.SaveChanges();

                return Created(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error: Post():{0}", typeof(Image).Name);
                return new StatusCodeResult(500);
            }
        }
        //[Authorize]
        [HttpGet]
        [EnableQuery(AllowedFunctions = AllowedFunctions.None, AllowedQueryOptions = AllowedQueryOptions.None)]
        public virtual async Task<Microsoft.AspNetCore.Mvc.FileContentResult> GetImage([FromODataUri] Guid imageId)
        {

            var image = _context.Images.Find(imageId);

            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(_config.GetConnectionString("AzureBlobStorage"));
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

            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(_config.GetConnectionString("AzureBlobStorage"));
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

        [Authorize]
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
                var model = (ImageDTO)parameters["model"];
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(_config.GetConnectionString("AzureBlobStorage"));
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
