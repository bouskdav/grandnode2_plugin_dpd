using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Shipping.DPD;

[assembly: PluginInfo(
    FriendlyName = "DPD Shipping",
    Group = "Shipping rate",
    SystemName = DPDShippingDefaults.ProviderSystemName,
    SupportedVersion = GrandVersion.SupportedPluginVersion,
    Author = "Laguna Solutions",
    Version = "1.00"
)]