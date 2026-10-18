namespace Erp.Modules.Inventory;

public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryProductBalance>> GetProductBalancesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Supplier>> GetSuppliersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Supplier>> GetSupplierCatalogItemsAsync(CancellationToken cancellationToken = default);
    Task<Supplier?> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<string> GetNextSupplierCodeAsync(CancellationToken cancellationToken = default);
    Task<int> AddSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default);
    Task UpdateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task<int> CreatePurchaseRequestAsync(int productId, decimal quantity, string? notes, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseRequestSummary>> GetPurchaseRequestsAsync(CancellationToken cancellationToken = default);
    Task RejectPurchaseRequestAsync(int requestId, CancellationToken cancellationToken = default);
    Task<int> ApprovePurchaseRequestAsync(int requestId, int supplierId, decimal approvedQty, decimal unitPrice, string paymentTerm, CancellationToken cancellationToken = default);
    Task<int> CreateDirectPurchaseAsync(int productId, int supplierId, decimal quantity, decimal unitPrice, string paymentTerm, string? notes, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseSummary>> GetPurchasesAsync(CancellationToken cancellationToken = default);
    Task MarkPurchaseDeliveredAsync(int purchaseId, CancellationToken cancellationToken = default);
    Task CancelPurchaseAsync(int purchaseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovementSummary>> GetStockEntriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovementSummary>> GetStockOutputsAsync(CancellationToken cancellationToken = default);
    Task<int> CreateStockEntryAsync(int productId, decimal quantity, string? notes, CancellationToken cancellationToken = default);
    Task<int> CreateStockOutputAsync(int productId, decimal quantity, string? notes, CancellationToken cancellationToken = default);
}
