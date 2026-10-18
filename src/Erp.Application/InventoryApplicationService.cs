using Erp.Modules.Inventory;

namespace Erp.Application;

public sealed class InventoryApplicationService
{
    private readonly IInventoryRepository _repository;

    public InventoryApplicationService(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<InventoryProductBalance>> GetProductBalancesAsync(CancellationToken cancellationToken = default)
        => _repository.GetProductBalancesAsync(cancellationToken);

    public Task<IReadOnlyList<Supplier>> GetSuppliersAsync(CancellationToken cancellationToken = default)
        => _repository.GetSuppliersAsync(cancellationToken);

    public Task<IReadOnlyList<Supplier>> GetSupplierCatalogItemsAsync(CancellationToken cancellationToken = default)
        => _repository.GetSupplierCatalogItemsAsync(cancellationToken);

    public Task<Supplier?> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default)
        => _repository.GetSupplierByIdAsync(id, cancellationToken);

    public Task<string> GetNextSupplierCodeAsync(CancellationToken cancellationToken = default)
        => _repository.GetNextSupplierCodeAsync(cancellationToken);

    public Task<int> AddSupplierAsync(string code, string name, string taxId, bool isActive, string? contactName,
        string? email, string? phone, string? street, string? number, string? district,
        string? city, string? state, string? zipCode, decimal minimumOrder, string? segment,
        CancellationToken cancellationToken = default)
    {
        var supplier = new Supplier
        {
            Code = code,
            Name = name,
            TaxId = taxId,
            IsActive = isActive,
            ContactName = contactName,
            Email = email,
            Phone = phone,
            Street = street,
            Number = number,
            District = district,
            City = city,
            State = state,
            ZipCode = zipCode,
            MinimumOrder = minimumOrder,
            Segment = segment
        };

        return _repository.AddSupplierAsync(supplier, cancellationToken);
    }

    public Task UpdateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default)
        => _repository.UpdateSupplierAsync(supplier, cancellationToken);

    public Task<int> CreatePurchaseRequestAsync(int productId, decimal quantity, string? notes, CancellationToken cancellationToken = default)
        => _repository.CreatePurchaseRequestAsync(productId, quantity, notes, cancellationToken);

    public Task<IReadOnlyList<PurchaseRequestSummary>> GetPurchaseRequestsAsync(CancellationToken cancellationToken = default)
        => _repository.GetPurchaseRequestsAsync(cancellationToken);

    public Task RejectPurchaseRequestAsync(int requestId, CancellationToken cancellationToken = default)
        => _repository.RejectPurchaseRequestAsync(requestId, cancellationToken);

    public Task<int> ApprovePurchaseRequestAsync(int requestId, int supplierId, decimal approvedQty, decimal unitPrice, string paymentTerm, CancellationToken cancellationToken = default)
        => _repository.ApprovePurchaseRequestAsync(requestId, supplierId, approvedQty, unitPrice, paymentTerm, cancellationToken);

    public Task<int> CreateDirectPurchaseAsync(int productId, int supplierId, decimal quantity, decimal unitPrice, string paymentTerm, string? notes, CancellationToken cancellationToken = default)
        => _repository.CreateDirectPurchaseAsync(productId, supplierId, quantity, unitPrice, paymentTerm, notes, cancellationToken);

    public Task<IReadOnlyList<PurchaseSummary>> GetPurchasesAsync(CancellationToken cancellationToken = default)
        => _repository.GetPurchasesAsync(cancellationToken);

    public Task MarkPurchaseDeliveredAsync(int purchaseId, CancellationToken cancellationToken = default)
        => _repository.MarkPurchaseDeliveredAsync(purchaseId, cancellationToken);

    public Task CancelPurchaseAsync(int purchaseId, CancellationToken cancellationToken = default)
        => _repository.CancelPurchaseAsync(purchaseId, cancellationToken);

    public Task<IReadOnlyList<StockMovementSummary>> GetStockEntriesAsync(CancellationToken cancellationToken = default)
        => _repository.GetStockEntriesAsync(cancellationToken);

    public Task<IReadOnlyList<StockMovementSummary>> GetStockOutputsAsync(CancellationToken cancellationToken = default)
        => _repository.GetStockOutputsAsync(cancellationToken);

    public Task<int> CreateStockEntryAsync(int productId, decimal quantity, string? notes, CancellationToken cancellationToken = default)
        => _repository.CreateStockEntryAsync(productId, quantity, notes, cancellationToken);

    public Task<int> CreateStockOutputAsync(int productId, decimal quantity, string? notes, CancellationToken cancellationToken = default)
        => _repository.CreateStockOutputAsync(productId, quantity, notes, cancellationToken);
}
