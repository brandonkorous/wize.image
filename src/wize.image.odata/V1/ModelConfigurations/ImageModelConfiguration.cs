using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wize.image.data.V1.Models;
using wize.image.odata.V1.Models;

namespace wize.image.odata.V1.ModelConfigurations
{
    public class ImageModelConfiguration : IModelConfiguration
    {
        public void Apply(ODataModelBuilder builder, ApiVersion version, string routePrefix)
        {
            switch (version.MajorVersion)
            {
                case 1:
                    BuildV1(builder);
                    break;
                default:
                    BuildDefault(builder);
                    break;
            }
        }

        private EntityTypeConfiguration<Image> BuildDefault(ODataModelBuilder builder)
        {
            var model = builder.EntitySet<Image>("Images").EntityType;
            model.HasKey(m => m.ImageId);
            
            builder.Namespace = "";
            builder.Action("GetImage").Returns<FileContentResult>().Parameter<Guid>("imageId");
            var action = builder.Action("GetSizedImage");
            action.Parameter<Guid>("imageId");
            action.Parameter<int>("width");
            action.Parameter<int>("height");
            action.Returns<FileContentResult>();
            builder.Action("UploadImage").Returns<Guid>().Parameter<ImageDTO>("model");
            return model;
        }

        private void BuildV1(ODataModelBuilder builder)
        {
            BuildDefault(builder);//.Ignore(something);
        }
    }
}
