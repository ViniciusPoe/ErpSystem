using Erp.Modules.Sales;

namespace Erp.Application;

public sealed class SalesApplicationService
{
    private readonly ISalesRepository _salesRepository;

    public SalesApplicationService(ISalesRepository salesRepository)
    {
        _salesRepository = salesRepository;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => _salesRepository.InitializeAsync(cancellationToken);

    public Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
        => _salesRepository.GetCustomersAsync(cancellationToken);

    public Task<IReadOnlyList<Customer>> GetCustomerCatalogItemsAsync(CancellationToken cancellationToken = default)
        => _salesRepository.GetCustomerCatalogItemsAsync(cancellationToken);

    public Task<Customer?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default)
        => _salesRepository.GetCustomerByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
        => _salesRepository.GetProductsAsync(cancellationToken);

    public Task<IReadOnlyList<Product>> GetProductCatalogItemsAsync(CancellationToken cancellationToken = default)
        => _salesRepository.GetProductCatalogItemsAsync(cancellationToken);

    public Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
        => _salesRepository.GetProductByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Carrier>> GetCarriersAsync(CancellationToken cancellationToken = default)
        => _salesRepository.GetCarriersAsync(cancellationToken);

    public Task<IReadOnlyList<Municipality>> GetMunicipalitiesAsync(CancellationToken cancellationToken = default)
        => _salesRepository.GetMunicipalitiesAsync(cancellationToken);

    public Task<int> AddMunicipalityAsync(string code, string name, string state, string country = "Brasil", CancellationToken cancellationToken = default)
    {
        var municipality = new Municipality { Code = code, Name = name.Trim(), State = state.Trim(), Country = country.Trim() };
        return _salesRepository.AddMunicipalityAsync(municipality, cancellationToken);
    }

    public Task<string> GetNextCustomerCodeAsync(CancellationToken cancellationToken = default)
        => _salesRepository.GetNextCustomerCodeAsync(cancellationToken);

    public Task<string> GetNextProductCodeAsync(CancellationToken cancellationToken = default)
        => _salesRepository.GetNextProductCodeAsync(cancellationToken);

    public Task UpdateMunicipalityAsync(Municipality municipality, CancellationToken cancellationToken = default)
        => _salesRepository.UpdateMunicipalityAsync(municipality, cancellationToken);

    public Task DeleteMunicipalityAsync(int id, CancellationToken cancellationToken = default)
        => _salesRepository.DeleteMunicipalityAsync(id, cancellationToken);

    public Task<int> AddCustomerAsync(string code, string name, string document, bool isActive, string? contactName, string? email, string? phone, string? street, string? number, string? district, string? city, string? state, string? zipCode, CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Code = code,
            Name = name,
            Document = document,
            IsActive = isActive,
            ContactName = contactName,
            Email = email,
            Phone = phone,
            Street = street,
            Number = number,
            District = district,
            City = city,
            State = state,
            ZipCode = zipCode
        };

        return _salesRepository.AddCustomerAsync(customer, cancellationToken);
    }

    public Task UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
        => _salesRepository.UpdateCustomerAsync(customer, cancellationToken);

    public Task<int> AddProductAsync(string sku, string name, decimal unitPrice, decimal quantity, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Sku = sku,
            Name = string.IsNullOrWhiteSpace(name) ? sku : name.Trim(),
            UnitPrice = unitPrice,
            Quantity = quantity
        };

        return _salesRepository.AddProductAsync(product, cancellationToken);
    }

    public Task<int> AddProductAsync(Product product, CancellationToken cancellationToken = default)
        => _salesRepository.AddProductAsync(product, cancellationToken);

    public Task UpdateProductAsync(Product product, CancellationToken cancellationToken = default)
        => _salesRepository.UpdateProductAsync(product, cancellationToken);

    public Task<int> SaveProductAsync(Product product, IReadOnlyList<(int FieldId, string Value)> values, CancellationToken cancellationToken = default)
        => _salesRepository.SaveProductAsync(product, values, cancellationToken);

    // Classifications
    public Task<IReadOnlyList<ProductClassification>> GetClassificationsAsync(CancellationToken ct = default) => _salesRepository.GetClassificationsAsync(ct);
    public Task<int> AddClassificationAsync(ProductClassification c, CancellationToken ct = default) => _salesRepository.AddClassificationAsync(c, ct);
    public Task UpdateClassificationAsync(ProductClassification c, CancellationToken ct = default) => _salesRepository.UpdateClassificationAsync(c, ct);
    public Task DeleteClassificationAsync(int id, CancellationToken ct = default) => _salesRepository.DeleteClassificationAsync(id, ct);

    public Task<IReadOnlyList<ProductSubClassification>> GetSubClassificationsAsync(int classificationId, CancellationToken ct = default) => _salesRepository.GetSubClassificationsAsync(classificationId, ct);
    public Task<IReadOnlyList<ProductSubClassification>> GetAllSubClassificationsAsync(CancellationToken ct = default) => _salesRepository.GetAllSubClassificationsAsync(ct);
    public Task<int> AddSubClassificationAsync(ProductSubClassification s, CancellationToken ct = default) => _salesRepository.AddSubClassificationAsync(s, ct);
    public Task UpdateSubClassificationAsync(ProductSubClassification s, CancellationToken ct = default) => _salesRepository.UpdateSubClassificationAsync(s, ct);
    public Task DeleteSubClassificationAsync(int id, CancellationToken ct = default) => _salesRepository.DeleteSubClassificationAsync(id, ct);

    public Task<IReadOnlyList<ProductCustomField>> GetCustomFieldsAsync(int subClassificationId, CancellationToken ct = default) => _salesRepository.GetCustomFieldsAsync(subClassificationId, ct);
    public Task<int> AddCustomFieldAsync(ProductCustomField f, CancellationToken ct = default) => _salesRepository.AddCustomFieldAsync(f, ct);
    public Task UpdateCustomFieldAsync(ProductCustomField f, CancellationToken ct = default) => _salesRepository.UpdateCustomFieldAsync(f, ct);
    public Task DeleteCustomFieldAsync(int id, CancellationToken ct = default) => _salesRepository.DeleteCustomFieldAsync(id, ct);

    public Task<IReadOnlyList<(int FieldId, string FieldName, string Value)>> GetProductCustomValuesAsync(int productId, CancellationToken ct = default) => _salesRepository.GetProductCustomValuesAsync(productId, ct);
    public Task SaveProductCustomValuesAsync(int productId, IReadOnlyList<(int FieldId, string Value)> values, CancellationToken ct = default) => _salesRepository.SaveProductCustomValuesAsync(productId, values, ct);

    // Brands
    public Task<IReadOnlyList<ProductBrand>> GetBrandsAsync(CancellationToken ct = default) => _salesRepository.GetBrandsAsync(ct);
    public Task<int> AddBrandAsync(ProductBrand b, CancellationToken ct = default) => _salesRepository.AddBrandAsync(b, ct);
    public Task UpdateBrandAsync(ProductBrand b, CancellationToken ct = default) => _salesRepository.UpdateBrandAsync(b, ct);
    public Task DeleteBrandAsync(int id, CancellationToken ct = default) => _salesRepository.DeleteBrandAsync(id, ct);

    public Task<int> AddCarrierAsync(string code, string name, string document, string? contactName,
        string? email, string? phone, string? street, string? number, string? district,
        string? city, string? state, string? zipCode, CancellationToken cancellationToken = default)
    {
        var carrier = new Carrier
        {
            Code = code,
            Name = name,
            Document = document,
            ContactName = contactName,
            Email = email,
            Phone = phone,
            Street = street,
            Number = number,
            District = district,
            City = city,
            State = state,
            ZipCode = zipCode
        };

        return _salesRepository.AddCarrierAsync(carrier, cancellationToken);
    }

    public Task UpdateCarrierAsync(Carrier carrier, CancellationToken cancellationToken = default)
        => _salesRepository.UpdateCarrierAsync(carrier, cancellationToken);

    public Task<int> SaveCarrierAsync(Carrier carrier, IReadOnlyList<int> municipalityIds, CancellationToken cancellationToken = default)
        => _salesRepository.SaveCarrierAsync(carrier, municipalityIds, cancellationToken);

    public Task<IReadOnlyList<Municipality>> GetCarrierServiceAreasAsync(int carrierId, CancellationToken cancellationToken = default)
        => _salesRepository.GetCarrierServiceAreasAsync(carrierId, cancellationToken);

    public Task SetCarrierServiceAreasAsync(int carrierId, IReadOnlyList<int> municipalityIds, CancellationToken cancellationToken = default)
        => _salesRepository.SetCarrierServiceAreasAsync(carrierId, municipalityIds, cancellationToken);

    public SalesTotals CalculateTotals(IReadOnlyList<SalesItemDraft> items, decimal discountAmount, decimal freightAmount)
        => SalesTaxCalculator.Calculate(items, discountAmount, freightAmount);

    public Task<int> CreateQuoteAsync(QuoteDraft draft, CancellationToken cancellationToken = default)
    {
        var totals = CalculateTotals(draft.Items, draft.DiscountAmount, draft.FreightAmount);
        return _salesRepository.CreateQuoteAsync(draft, totals, cancellationToken);
    }

    public Task UpdateQuoteAsync(int quoteId, QuoteDraft draft, CancellationToken cancellationToken = default)
    {
        var totals = CalculateTotals(draft.Items, draft.DiscountAmount, draft.FreightAmount);
        return _salesRepository.UpdateQuoteAsync(quoteId, draft, totals, cancellationToken);
    }

    public Task<QuoteData?> GetQuoteAsync(int quoteId, CancellationToken cancellationToken = default)
        => _salesRepository.GetQuoteAsync(quoteId, cancellationToken);

    public Task<IReadOnlyList<QuoteSummary>> GetQuotesAsync(CancellationToken cancellationToken = default)
        => _salesRepository.GetQuotesAsync(cancellationToken);

    public Task<int> ApproveQuoteAsync(int quoteId, string paymentTerm, int? carrierId = null, decimal freightAmount = 0, string freightType = "CIF", CancellationToken cancellationToken = default)
        => _salesRepository.ApproveQuoteAsync(quoteId, paymentTerm, carrierId, freightAmount, freightType, cancellationToken);

    public Task RejectQuoteAsync(int quoteId, CancellationToken cancellationToken = default)
        => _salesRepository.RejectQuoteAsync(quoteId, cancellationToken);

    public Task<int> CreateOrderAsync(OrderDraft draft, CancellationToken cancellationToken = default)
    {
        var totals = CalculateTotals(draft.Items, draft.DiscountAmount, draft.FreightAmount);
        return _salesRepository.CreateOrderAsync(draft, totals, cancellationToken);
    }

    public Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(CancellationToken cancellationToken = default)
        => _salesRepository.GetOrdersAsync(cancellationToken);

    public Task MarkOrderReadyForInvoiceAsync(int orderId, CancellationToken cancellationToken = default)
        => _salesRepository.MarkOrderReadyForInvoiceAsync(orderId, cancellationToken);

    public Task<int> GenerateInvoiceAsync(int orderId, CancellationToken cancellationToken = default)
        => _salesRepository.GenerateInvoiceAsync(orderId, cancellationToken);

    public Task<int> CreateDirectInvoiceAsync(OrderDraft draft, CancellationToken cancellationToken = default)
    {
        var totals = CalculateTotals(draft.Items, draft.DiscountAmount, draft.FreightAmount);
        return _salesRepository.CreateDirectInvoiceAsync(draft, totals, cancellationToken);
    }

    public Task<IReadOnlyList<InvoiceSummary>> GetInvoicesAsync(CancellationToken cancellationToken = default)
        => _salesRepository.GetInvoicesAsync(cancellationToken);
}
