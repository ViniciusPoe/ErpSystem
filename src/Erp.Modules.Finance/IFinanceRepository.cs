namespace Erp.Modules.Finance;

public interface IFinanceRepository
{
    Task<IReadOnlyList<AccountReceivableSummary>> GetAccountsReceivableAsync(CancellationToken cancellationToken = default);
    Task MarkReceivablePaidAsync(int receivableId, CancellationToken cancellationToken = default);
    Task<int> CreateManualReceivableAsync(int? customerId, decimal totalAmount, string paymentTerm, DateTime dueBaseDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountPayableSummary>> GetAccountsPayableAsync(CancellationToken cancellationToken = default);
    Task MarkPayablePaidAsync(int payableId, CancellationToken cancellationToken = default);

    Task<int> CreateManualPayableAsync(string description, int? supplierId, decimal totalAmount, string paymentTerm, DateTime dueBaseDate, CancellationToken cancellationToken = default);
    Task<int> CreateRecurringPayableAsync(string description, int? supplierId, decimal monthlyAmount, DateTime startDate, int months, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecurringPayableSummary>> GetRecurringPayablesAsync(CancellationToken cancellationToken = default);
    Task CancelRecurringPayableAsync(int recurringId, CancellationToken cancellationToken = default);
}
