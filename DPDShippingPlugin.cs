using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure.Plugins;

namespace Shipping.DPD
{
    public class DPDShippingPlugin : BasePlugin, IPlugin
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        #endregion

        #region Ctor
        public DPDShippingPlugin(
            ISettingService settingService,
            ITranslationService translationService,
            ILanguageService languageService)
        {
            _settingService = settingService;
            _translationService = translationService;
            _languageService = languageService;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Install plugin
        /// </summary>
        public override async Task Install()
        {
            //settings
            var settings = new DPDShippingSettings {
                LimitMethodsToCreated = false,
            };
            await _settingService.SaveSetting(settings);

            //locales
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.DPD.FriendlyName", "DPD Pickup");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.DPD.PluginName", "DPD Pickup");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.DPD.PluginDescription", "Pick up at selected DPD Pickup point");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.DPD.SelectBeforeProceed", "Please select a Pickup point before proceed");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Shipping.DPD.ShippingPointName", "Selected Pickup point");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.Store", "Store");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.Warehouse", "Warehouse");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.Country", "Country");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.StateProvince", "State / province");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.Zip", "Zip");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.ShippingMethod", "Shipping method");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.From", "Order weight from");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.To", "Order weight to");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.AdditionalFixedCost", "Additional fixed cost");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.LowerWeightLimit", "Lower weight limit");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.PercentageRateOfSubtotal", "Charge percentage (of subtotal)");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.RatePerWeightUnit", "Rate per weight unit");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.LimitMethodsToCreated", "Limit shipping methods to configured ones");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.DataHtml", "Data");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Fields.DisplayOrder", "DisplayOrder");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.AddRecord", "Add record");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Formula", "Formula to calculate rates");
            await this.AddOrUpdatePluginTranslateResource(_translationService, _languageService, "Plugins.Shipping.DPD.Formula.Value", "[additional fixed cost] + ([order total weight] - [lower weight limit]) * [rate per weight unit] + [order subtotal] * [charge percentage]");

            await base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override async Task Uninstall()
        {
            await base.Uninstall();
        }

        /// <summary>
        /// Returns a value indicating whether shipping methods should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public async Task<bool> HideShipmentMethods(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this shipping methods if all products in the cart are downloadable
            //or hide this shipping methods if current customer is from certain country
            return await Task.FromResult(false);
        }

        #endregion

    }

}
