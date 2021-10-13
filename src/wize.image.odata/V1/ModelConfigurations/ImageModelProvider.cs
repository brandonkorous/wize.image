using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wize.image.data.V1.Models;
using wize.image.odata.V1.ModelConfigurations.Interfaces;
using wize.image.odata.V1.Models;

namespace wize.image.odata.V1.ModelConfigurations
{
    public class ImageModelProvider : IODataModelProvider
    {
        private IDictionary<string, IEdmModel> _cached = new Dictionary<string, IEdmModel>();
        public IEdmModel GetEdmModel(string apiVersion)
        {
            if (_cached.TryGetValue(apiVersion, out IEdmModel model))
            {
                return model;
            }

            model = BuildEdmModel(apiVersion);
            _cached[apiVersion] = model;
            return model;
        }

        private static IEdmModel BuildEdmModel(string version)
        {
            switch (version)
            {
                case "1.0":
                    BuildV1();
                    break;
                default:
                    BuildDefault(new ODataConventionModelBuilder());
                    break;
            }

            throw new NotSupportedException($"The input version '{version}' is not supported!");
        }

        private static IEdmModel BuildDefault(ODataConventionModelBuilder builder)
        {
            var model = builder.EntitySet<Image>("Images").EntityType;
            model.HasKey(m => m.ImageId);

            builder.Namespace = "";
            builder.Action("GetImage").Returns<FileContentResult>().Parameter<Guid>("imageId");
            var action = builder.Action("GetSizedImage");
            action.Parameter<Guid>("imageId");
            action.Parameter<int>("width");
            action.Parameter<int>("height");
            action.Returns<IActionResult>();
            builder.Action("UploadImage").Returns<Guid>().Parameter<ImageDTO>("model");
            return builder.GetEdmModel();
        }

        private static IEdmModel BuildV1()
        {
            var builder = new ODataConventionModelBuilder();
            return BuildDefault(builder);//.Ignore(something);
        }
    }
}
