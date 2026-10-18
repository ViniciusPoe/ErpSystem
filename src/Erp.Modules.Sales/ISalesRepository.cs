namespace Erp.Modules.Sales;

public interface ISalesRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> GetCustomerCatalogItemsAsync(CancellationToken cancellationToken = default);
    Task<Customer?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetProductCatalogItemsAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Carrier>> GetCarriersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Municipality>> GetMunicipalitiesAsync(CancellationToken cancellationToken = default);
    Task<string> GetNextCustomerCodeAsync(CancellationToken cancellationToken = default);
    Task<string> GetNextProductCodeAsync(CancellationToken cancellationToken = default);

    Task<int> AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default);
    Task UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default);

    Task<int> AddProductAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateProductAsync(Product product, CancellationToken cancellationToken = default);
    Task<int> SaveProductAsync(Product product, IReadOnlyList<(int FieldId, string Value)> values, CancellationToken cancellationToken = default);

    Task<int> AddCarrierAsync(Carrier carrier, CancellationToken cancellationToken = default);
    Task UpdateCarrierAsync(Carrier carrier, CancellationToken cancellationToken = default);
    Task<int> SaveCarrierAsync(Carrier carrier, IReadOnlyList<int> municipalityIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Municipality>> GetCarrierServiceAreasAsync(int carrierId, CancellationToken cancellationToken = default);
    Task SetCarrierServiceAreasAsync(int carrierId, IReadOnlyList<int> municipalityIds, CancellationToken cancellationToken = default);

    Task<int> AddMunicipalityAsync(Municipality municipality, CancellationToken cancellationToken = default);
    Task UpdateMunicipalityAsync(Municipality municipality, CancellationToken cancellationToken = default);
    Task DeleteMunicipalityAsync(int id, CancellationToken cancellationToken = default);

    // Product classifications
    Task<IReadOnlyList<ProductClassification>> GetClassificationsAsync(CancellationToken cancellationToken = default);
    Task<int> AddClassificationAsync(ProductClassification classification, CancellationToken cancellationToken = default);
    Task UpdateClassificationAsync(ProductClassification classification, CancellationToken cancellationToken = default);
    Task DeleteClassificationAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductSubClassification>> GetSubClassificationsAsync(int classificationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductSubClassification>> GetAllSubClassificationsAsync(CancellationToken cancellationToken = default);
    Task<int> AddSubClassificationAsync(ProductSubClassification sub, CancellationToken cancellationToken = default);
    Task UpdateSubClassificationAsync(ProductSubClassification sub, CancellationToken cancellationToken = default);
    Task DeleteSubClassificationAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductCustomField>> GetCustomFieldsAsync(int subClassificationId, CancellationToken cancellationToken = default);
    Task<int> AddCustomFieldAsync(ProductCustomField field, CancellationToken cancellationToken = default);
    Task UpdateCustomFieldAsync(ProductCustomField field, CancellationToken cancellationToken = default);
    Task DeleteCustomFieldAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(int FieldId, string FieldName, string Value)>> GetProductCustomValuesAsync(int productId, CancellationToken cancellationToken = default);
    Task SaveProductCustomValuesAsync(int productId, IReadOnlyList<(int FieldId, string Value)> values, CancellationToken cancellationToken = default);

    // Product brands
    Task<IReadOnlyList<ProductBrand>> GetBrandsAsync(CancellationToken cancellationToken = default);
    Task<int> AddBrandAsync(ProductBrand brand, CancellationToken cancellationToken = default);
    Task UpdateBrandAsync(ProductBrand brand, CancellationToken cancellationToken = default);
    Task DeleteBrandAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateQuoteAsync(QuoteDraft draft, SalesTotals totals, CancellationToken cancellationToken = default);
    Task UpdateQuoteAsync(int quoteId, QuoteDraft draft, SalesTotals totals, CancellationToken cancellationToken = default);
    Task<QuoteData?> GetQuoteAsync(int quoteId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuoteSummary>> GetQuotesAsync(CancellationToken cancellationToken = default);
    Task<int> ApproveQuoteAsync(int quoteId, string paymentTerm, int? carrierId, decimal freightAmount, string freightType, CancellationToken cancellationToken = default);
    Task RejectQuoteAsync(int quoteId, CancellationToken cancellationToken = default);

    Task<int> CreateOrderAsync(OrderDraft draft, SalesTotals totals, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(CancellationToken cancellationToken = default);
    Task MarkOrderReadyForInvoiceAsync(int orderId, CancellationToken cancellationToken = default);

    Task<int> GenerateInvoiceAsync(int orderId, CancellationToken cancellationToken = default);
    Task<int> CreateDirectInvoiceAsync(OrderDraft draft, SalesTotals totals, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InvoiceSummary>> GetInvoicesAsync(CancellationToken cancellationToken = default);
}
