using Microsoft.OData.Edm;

namespace wize.image.odata.V1.ModelConfigurations.Interfaces
{
    public interface IODataModelProvider
    {
        IEdmModel GetEdmModel(string apiVersion);
    }
}
