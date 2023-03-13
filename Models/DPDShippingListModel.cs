using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Shipping.DPD.Models
{
    public class DPDShippingListModel : BaseModel
    {
        [GrandResourceDisplayName("Plugins.Shipping.DPD.Fields.LimitMethodsToCreated")]
        public bool LimitMethodsToCreated { get; set; }

        [GrandResourceDisplayName("Plugins.Shipping.DPD.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}