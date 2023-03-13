using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Shipping.DPD
{
    public partial class EndpointProvider : IEndpointProvider
    {
        public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("Plugins.DPD.Points",
                 "Plugins/DPD/Points",
                 new { controller = "DPDShipping", action = "Points" }
            );
        }

        public int Priority => 0;
    }
}
