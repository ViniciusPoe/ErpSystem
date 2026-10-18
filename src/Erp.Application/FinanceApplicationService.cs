using Erp.Modules.Finance;

namespace Erp.Application;

public sealed class FinanceApplicationService
{
    private readonly IFinanceRepository _repository;

    public FinanceApplicationService(IFinanceRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<AccountReceivableSummary>> GetAccountsReceivableAsync(CancellationToken cancellationToken = default)
        => _repository.GetAccountsReceivableAsync(cancellationToken);

    public Task MarkReceivablePaidAsync(int receivableId, CancellationToken cancellationToken = default)
        => _repository.MarkReceivablePaidAsync(receivableId, cancellationToken);

    public Task<int> CreateManualReceivableAsync(int? customerId, decimal totalAmount, string paymentTerm, DateTime dueBaseDate, CancellationToken cancellationToken = default)
        => _repository.CreateManualReceivableAsync(customerId, totalAmount, paymentTerm, dueBaseDate, cancellationToken);

    public Task<IReadOnlyList<AccountPayableSummary>> GetAccountsPayableAsync(CancellationToken cancellationToken = default)
        => _repository.GetAccountsPayableAsync(cancellationToken);

    public Task MarkPayablePaidAsync(int payableId, CancellationToken cancellationToken = default)
        => _repository.MarkPayablePaidAsync(payableId, cancellationToken);

    public Task<int> CreateManualPayableAsync(string description, int? supplierId, decimal totalAmount, string paymentTerm, DateTime dueBaseDate, CancellationToken cancellationToken = default)
        => _repository.CreateManualPayableAsync(description, supplierId, totalAmount, paymentTerm, dueBaseDate, cancellationToken);

    public Task<int> CreateRecurringPayableAsync(string description, int? supplierId, decimal monthlyAmount, DateTime startDate, int months, CancellationToken cancellationToken = default)
        => _repository.CreateRecurringPayableAsync(description, supplierId, monthlyAmount, startDate, months, cancellationToken);

    public Task<IReadOnlyList<RecurringPayableSummary>> GetRecurringPayablesAsync(CancellationToken cancellationToken = default)
        => _repository.GetRecurringPayablesAsync(cancellationToken);

    public Task CancelRecurringPayableAsync(int recurringId, CancellationToken cancellationToken = default)
        => _repository.CancelRecurringPayableAsync(recurringId, cancellationToken);
}
