using Grand.Domain.Configuration;

namespace Shipping.DPD
{
    public class DPDShippingSettings : ISettings
    {
        public bool LimitMethodsToCreated { get; set; }
        public int DisplayOrder { get; set; }

    }
}
