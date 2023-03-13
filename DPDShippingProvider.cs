using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shipping.DPD.Services;
using System.Xml.Serialization;
using Shipping.DPD.Models;
using MongoDB.Bson.IO;

namespace Shipping.DPD
{
    public class DPDShippingCalcPlugin : IShippingRateCalculationProvider
    {
        #region Fields

        private readonly IShippingMethodService _shippingMethodService;
        private readonly IWorkContext _workContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITranslationService _translationService;
        private readonly IProductService _productService;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly IUserFieldService _userFieldService;
        private readonly ICurrencyService _currencyService;
        private readonly DPDShippingSettings _dpdShippingSettings;

        #endregion

        #region Ctor
        public DPDShippingCalcPlugin(
            IShippingMethodService shippingMethodService,
            IWorkContext workContext,
            ITranslationService translationService,
            IProductService productService,
            IServiceProvider serviceProvider,
            ICheckoutAttributeParser checkoutAttributeParser,
            IUserFieldService userFieldService,
            ICurrencyService currencyService,
            DPDShippingSettings dpdShippingSettings)
        {
            _shippingMethodService = shippingMethodService;
            _workContext = workContext;
            _translationService = translationService;
            _productService = productService;
            _serviceProvider = serviceProvider;
            _checkoutAttributeParser = checkoutAttributeParser;
            _userFieldService = userFieldService;
            _currencyService = currencyService;
            _dpdShippingSettings = dpdShippingSettings;
        }
        #endregion

        #region Utilities

        private async Task<double?> GetRate(double subTotal, double weight, string shippingMethodId,
            string storeId, string warehouseId, string countryId, string stateProvinceId, string zip)
        {
            return 0;
            //var shippingDPDService = _serviceProvider.GetRequiredService<IDPDShippingService>();
            //var shippingDPDSettings = _serviceProvider.GetRequiredService<DPDShippingSettings>();

            //var shippingDPDRecord = await shippingDPDService.FindRecord(shippingMethodId,
            //    storeId, warehouseId, countryId, stateProvinceId, zip, weight);
            //if (shippingDPDRecord == null)
            //{
            //    if (shippingDPDSettings.LimitMethodsToCreated)
            //        return null;

            //    return 0;
            //}

            ////additional fixed cost
            //double shippingTotal = shippingDPDRecord.AdditionalFixedCost;
            ////charge amount per weight unit
            //if (shippingDPDRecord.RatePerWeightUnit > 0)
            //{
            //    var weightRate = weight - shippingDPDRecord.LowerWeightLimit;
            //    if (weightRate < 0)
            //        weightRate = 0;
            //    shippingTotal += shippingDPDRecord.RatePerWeightUnit * weightRate;
            //}
            ////percentage rate of subtotal
            //if (shippingDPDRecord.PercentageRateOfSubtotal > 0)
            //{
            //    shippingTotal += Math.Round((double)((((float)subTotal) * ((float)shippingDPDRecord.PercentageRateOfSubtotal)) / 100f), 2);
            //}

            //if (shippingTotal < 0)
            //    shippingTotal = 0;
            //return shippingTotal;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets shopping cart item weight (of one item)
        /// </summary>
        /// <param name="shoppingCartItem">Shopping cart item</param>
        /// <returns>Shopping cart item weight</returns>
        private async Task<double> GetShoppingCartItemWeight(ShoppingCartItem shoppingCartItem)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException(nameof(shoppingCartItem));

            var product = await _productService.GetProductById(shoppingCartItem.ProductId);
            if (product == null)
                return 0;

            //attribute weight
            double attributesTotalWeight = 0;
            if (shoppingCartItem.Attributes != null && shoppingCartItem.Attributes.Any())
            {
                var attributeValues = product.ParseProductAttributeValues(shoppingCartItem.Attributes);
                foreach (var attributeValue in attributeValues)
                {
                    switch (attributeValue.AttributeValueTypeId)
                    {
                        case AttributeValueType.Simple:
                            {
                                //simple attribute
                                attributesTotalWeight += attributeValue.WeightAdjustment;
                            }
                            break;
                        case AttributeValueType.AssociatedToProduct:
                            {
                                //bundled product
                                var associatedProduct = await _productService.GetProductById(attributeValue.AssociatedProductId);
                                if (associatedProduct != null && associatedProduct.IsShipEnabled)
                                {
                                    attributesTotalWeight += associatedProduct.Weight * attributeValue.Quantity;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            var weight = product.Weight + attributesTotalWeight;
            return weight;
        }
        /// <summary>
        /// Gets shopping cart weight
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="includeCheckoutAttributes">A value indicating whether we should calculate weights of selected checkotu attributes</param>
        /// <returns>Total weight</returns>
        private async Task<double> GetTotalWeight(GetShippingOptionRequest request, bool includeCheckoutAttributes = true)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Customer customer = request.Customer;

            double totalWeight = 0;
            //shopping cart items
            foreach (var packageItem in request.Items)
                totalWeight += await GetShoppingCartItemWeight(packageItem.ShoppingCartItem) * packageItem.GetQuantity();

            //checkout attributes
            if (customer != null && includeCheckoutAttributes)
            {
                var checkoutAttributes = customer.GetUserFieldFromEntity<List<CustomAttribute>>(SystemCustomerFieldNames.CheckoutAttributes, request.StoreId);
                if (checkoutAttributes.Any())
                {
                    var attributeValues = await _checkoutAttributeParser.ParseCheckoutAttributeValues(checkoutAttributes);
                    foreach (var attributeValue in attributeValues)
                        totalWeight += attributeValue.WeightAdjustment;
                }
            }
            return totalWeight;
        }



        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public Task<GetShippingOptionResponse> GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException(nameof(getShippingOptionRequest));

            var response = new GetShippingOptionResponse();

            response.ShippingOptions.Add(new ShippingOption() {
                Name = _translationService.GetResource("Shipping.DPD.PluginName"),
                Description = _translationService.GetResource("Shipping.DPD.PluginDescription"),
                Rate = 0,
                ShippingRateProviderSystemName = "Shipping.DPD"
            });

            return Task.FromResult(response);
        }

        /// <summary>
        /// Gets fixed shipping rate (if Shipping rate  method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
        public async Task<double?> GetFixedRate(GetShippingOptionRequest getShippingOptionRequest)
        {
            return await Task.FromResult(default(double?));
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

        #region Properties


        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        public IShipmentTracker ShipmentTracker => null;

        public ShippingRateCalculationType ShippingRateCalculationType => ShippingRateCalculationType.Off;

        public string ConfigurationUrl => DPDShippingDefaults.ConfigurationUrl;

        public string SystemName => DPDShippingDefaults.ProviderSystemName;

        public string FriendlyName => _translationService.GetResource(DPDShippingDefaults.FriendlyName);

        public int Priority => _dpdShippingSettings.DisplayOrder;

        public IList<string> LimitedToStores => new List<string>();

        public IList<string> LimitedToGroups => new List<string>();

        public async Task<IList<string>> ValidateShippingForm(string shippingOption, IDictionary<string, string> data)
        {
            ////you can implement here any validation logic
            //return await Task.FromResult(new List<string>());

            // id of selected place
            data.TryGetValue("selectedShippingOptionId", out string shippingOptionId);
            // description of selected place
            data.TryGetValue("selectedShippingOptionDescription", out string shippingOptionDescription);
            // whole json object of selected place
            data.TryGetValue("selectedShippingOptionObject", out string shippingOptionObjectJson);

            var shippingMethodName = shippingOption?.Split(new[] { ':' })[0];

            if (string.IsNullOrEmpty(shippingOptionId))
                return new List<string>() { _translationService.GetResource("Shipping.DPD.SelectBeforeProceed") };

            if (shippingMethodName != _translationService.GetResource("Shipping.DPD.PluginName"))
                throw new ArgumentException("shippingMethodName");

            //var chosenShippingOption = await _shippingPointService.GetStoreShippingPointById(shippingOptionId);
            //if (chosenShippingOption == null)
            //    return new List<string>() { _translationService.GetResource("Shipping.ShippingPoint.SelectBeforeProceed") };

            //override price 
            var offeredShippingOptions = await _workContext.CurrentCustomer.GetUserField<List<ShippingOption>>(_userFieldService, SystemCustomerFieldNames.OfferedShippingOptions, _workContext.CurrentStore.Id);
            //offeredShippingOptions.Find(x => x.Name == shippingMethodName).Rate = await _currencyService.ConvertFromPrimaryStoreCurrency(chosenShippingOption.PickupFee, _workContext.WorkingCurrency);

            // save offered shipping options
            await _userFieldService.SaveField(
                _workContext.CurrentCustomer,
                SystemCustomerFieldNames.OfferedShippingOptions,
                offeredShippingOptions,
                _workContext.CurrentStore.Id
            );

            //var forCustomer =
            //    $"<strong>{_translationService.GetResource("Shipping.ShippingPoint.Fields.ShippingPointName")}:</strong> {chosenShippingOption.ShippingPointName}<br><strong>{_translationService.GetResource("Shipping.ShippingPoint.Fields.Description")}:</strong> {chosenShippingOption.Description}<br>";

            string shippingPointDescription = shippingOptionDescription;

            // save shipping point description (how it will be displayed on order page)
            await _userFieldService.SaveField(
                _workContext.CurrentCustomer,
                SystemCustomerFieldNames.ShippingOptionAttributeDescription,
                shippingPointDescription,
                _workContext.CurrentStore.Id
            );

            //var serializedObject = new Domain.ShippingPointSerializable() {
            //    Id = chosenShippingOption.Id,
            //    ShippingPointName = chosenShippingOption.ShippingPointName,
            //    Description = chosenShippingOption.Description,
            //    OpeningHours = chosenShippingOption.OpeningHours,
            //    PickupFee = chosenShippingOption.PickupFee,
            //    Country = (await _countryService.GetCountryById(chosenShippingOption.CountryId))?.Name,
            //    City = chosenShippingOption.City,
            //    Address1 = chosenShippingOption.Address1,
            //    ZipPostalCode = chosenShippingOption.ZipPostalCode,
            //    StoreId = chosenShippingOption.StoreId,
            //};

            DPDPointModel serializedObject = Newtonsoft.Json.JsonConvert.DeserializeObject<DPDPointModel>(shippingOptionObjectJson);

            var stringBuilder = new StringBuilder();
            string serializedAttribute;
            await using (var tw = new StringWriter(stringBuilder))
            {
                var xmlS = new XmlSerializer(typeof(DPDPointModel));
                xmlS.Serialize(tw, serializedObject);
                serializedAttribute = stringBuilder.ToString();
            }

            await _userFieldService.SaveField(
                _workContext.CurrentCustomer,
                SystemCustomerFieldNames.ShippingOptionAttribute,
                serializedAttribute,
                _workContext.CurrentStore.Id
            );

            return new List<string>();
        }

        public async Task<string> GetControllerRouteName()
        {
            return await Task.FromResult("Plugins.DPD.Points");
        }

        #endregion
    }

}
