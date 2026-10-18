using Erp.Infrastructure.MySql.Data;
using Erp.Infrastructure.MySql.Utilities;
using Erp.Modules.Finance;
using MySqlConnector;

namespace Erp.Infrastructure.MySql.Repositories;

public sealed class MySqlFinanceRepository : IFinanceRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlFinanceRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<AccountReceivableSummary>> GetAccountsReceivableAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT r.id, r.order_id, r.invoice_id, COALESCE(c.name, 'Cliente nao informado') customer_name, r.installment_number, r.due_date, r.amount, r.payment_term, r.status
FROM finance_receivables r
LEFT JOIN sales_customers c ON c.id = r.customer_id
ORDER BY r.due_date, r.id";

        var list = new List<AccountReceivableSummary>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new AccountReceivableSummary
            {
                Id = reader.GetInt32("id"),
                OrderId = NullableInt(reader, "order_id"),
                InvoiceId = NullableInt(reader, "invoice_id"),
                CustomerName = reader.GetString("customer_name"),
                InstallmentNumber = reader.GetInt32("installment_number"),
                DueDate = reader.GetDateTime("due_date"),
                Amount = reader.GetDecimal("amount"),
                PaymentTerm = reader.GetString("payment_term"),
                Status = reader.GetString("status")
            });
        }

        return list;
    }

    public async Task<int> CreateManualReceivableAsync(int? customerId, decimal totalAmount, string paymentTerm, DateTime dueBaseDate, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        var normalizedTerm = string.IsNullOrWhiteSpace(paymentTerm) ? "A_VISTA" : paymentTerm.Trim();
        var installments = PaymentInstallmentCalculator.BuildInstallments(totalAmount, dueBaseDate, normalizedTerm);

        const string sql = @"
INSERT INTO finance_receivables (order_id, invoice_id, customer_id, installment_number, due_date, amount, payment_term, status)
VALUES (NULL, NULL, @customerId, @installment, @dueDate, @amount, @paymentTerm, 'PENDENTE');
SELECT LAST_INSERT_ID();";

        int firstId = 0;
        foreach (var installment in installments)
        {
            await using var cmd = new MySqlCommand(sql, connection, (MySqlTransaction)tx);
            cmd.Parameters.AddWithValue("@customerId", customerId.HasValue ? customerId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@installment", installment.Number);
            cmd.Parameters.AddWithValue("@dueDate", installment.DueDate);
            cmd.Parameters.AddWithValue("@amount", installment.Amount);
            cmd.Parameters.AddWithValue("@paymentTerm", normalizedTerm);
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
            if (firstId == 0) firstId = id;
        }

        await tx.CommitAsync(cancellationToken);
        return firstId;
    }

    public async Task MarkReceivablePaidAsync(int receivableId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE finance_receivables SET status='PAGO', paid_at=NOW() WHERE id=@id AND status='PENDENTE'";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", receivableId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountPayableSummary>> GetAccountsPayableAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT p.id, p.purchase_id, p.supplier_id, s.name supplier_name, p.description, p.installment_number, p.due_date, p.amount, p.payment_term, p.status, p.recurring_id
FROM finance_payables p
LEFT JOIN inventory_suppliers s ON s.id = p.supplier_id
ORDER BY p.due_date, p.id";

        var list = new List<AccountPayableSummary>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new AccountPayableSummary
            {
                Id = reader.GetInt32("id"),
                PurchaseId = NullableInt(reader, "purchase_id"),
                SupplierId = NullableInt(reader, "supplier_id"),
                SupplierName = NullableString(reader, "supplier_name"),
                Description = reader.GetString("description"),
                InstallmentNumber = reader.GetInt32("installment_number"),
                DueDate = reader.GetDateTime("due_date"),
                Amount = reader.GetDecimal("amount"),
                PaymentTerm = reader.GetString("payment_term"),
                Status = reader.GetString("status"),
                RecurringId = NullableInt(reader, "recurring_id")
            });
        }

        return list;
    }

    public async Task MarkPayablePaidAsync(int payableId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE finance_payables SET status='PAGO', paid_at=NOW() WHERE id=@id AND status='PENDENTE'";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", payableId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> CreateManualPayableAsync(string description, int? supplierId, decimal totalAmount, string paymentTerm, DateTime dueBaseDate, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        var installments = PaymentInstallmentCalculator.BuildInstallments(totalAmount, dueBaseDate, paymentTerm);
        var normalizedTerm = string.IsNullOrWhiteSpace(paymentTerm) ? "A_VISTA" : paymentTerm.Trim();

        const string sql = @"
INSERT INTO finance_payables (purchase_id, supplier_id, recurring_id, description, installment_number, due_date, amount, payment_term, status)
VALUES (NULL, @supplierId, NULL, @description, @installment, @dueDate, @amount, @paymentTerm, 'PENDENTE');
SELECT LAST_INSERT_ID();";

        int firstId = 0;
        foreach (var installment in installments)
        {
            await using var cmd = new MySqlCommand(sql, connection, (MySqlTransaction)tx);
            cmd.Parameters.AddWithValue("@supplierId", supplierId.HasValue ? supplierId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@installment", installment.Number);
            cmd.Parameters.AddWithValue("@dueDate", installment.DueDate);
            cmd.Parameters.AddWithValue("@amount", installment.Amount);
            cmd.Parameters.AddWithValue("@paymentTerm", normalizedTerm);
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
            if (firstId == 0)
            {
                firstId = id;
            }
        }

        await tx.CommitAsync(cancellationToken);
        return firstId;
    }

    public async Task<int> CreateRecurringPayableAsync(string description, int? supplierId, decimal monthlyAmount, DateTime startDate, int months, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        const string recurringSql = @"
INSERT INTO finance_payable_recurring (supplier_id, description, monthly_amount, start_date, is_active)
VALUES (@supplierId, @description, @monthlyAmount, @startDate, 1);
SELECT LAST_INSERT_ID();";

        int recurringId;
        await using (var recurring = new MySqlCommand(recurringSql, connection, (MySqlTransaction)tx))
        {
            recurring.Parameters.AddWithValue("@supplierId", supplierId.HasValue ? supplierId.Value : DBNull.Value);
            recurring.Parameters.AddWithValue("@description", description);
            recurring.Parameters.AddWithValue("@monthlyAmount", monthlyAmount);
            recurring.Parameters.AddWithValue("@startDate", startDate.Date);
            recurringId = Convert.ToInt32(await recurring.ExecuteScalarAsync(cancellationToken));
        }

        const string payableSql = @"
INSERT INTO finance_payables (purchase_id, supplier_id, recurring_id, description, installment_number, due_date, amount, payment_term, status)
VALUES (NULL, @supplierId, @recurringId, @description, @installment, @dueDate, @amount, 'MENSAL', 'PENDENTE');";

        for (var i = 0; i < Math.Max(1, months); i++)
        {
            await using var payable = new MySqlCommand(payableSql, connection, (MySqlTransaction)tx);
            payable.Parameters.AddWithValue("@supplierId", supplierId.HasValue ? supplierId.Value : DBNull.Value);
            payable.Parameters.AddWithValue("@recurringId", recurringId);
            payable.Parameters.AddWithValue("@description", description);
            payable.Parameters.AddWithValue("@installment", i + 1);
            payable.Parameters.AddWithValue("@dueDate", startDate.Date.AddMonths(i));
            payable.Parameters.AddWithValue("@amount", monthlyAmount);
            await payable.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
        return recurringId;
    }

    public async Task<IReadOnlyList<RecurringPayableSummary>> GetRecurringPayablesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT r.id, r.description, r.supplier_id, s.name supplier_name, r.monthly_amount, r.start_date, r.is_active
FROM finance_payable_recurring r
LEFT JOIN inventory_suppliers s ON s.id = r.supplier_id
ORDER BY r.id DESC";

        var list = new List<RecurringPayableSummary>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new RecurringPayableSummary
            {
                Id = reader.GetInt32("id"),
                Description = reader.GetString("description"),
                SupplierId = NullableInt(reader, "supplier_id"),
                SupplierName = NullableString(reader, "supplier_name"),
                MonthlyAmount = reader.GetDecimal("monthly_amount"),
                StartDate = reader.GetDateTime("start_date"),
                IsActive = reader.GetBoolean("is_active")
            });
        }

        return list;
    }

    public async Task CancelRecurringPayableAsync(int recurringId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        await using (var cancel = new MySqlCommand("UPDATE finance_payable_recurring SET is_active=0, canceled_at=NOW() WHERE id=@id", connection, (MySqlTransaction)tx))
        {
            cancel.Parameters.AddWithValue("@id", recurringId);
            await cancel.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteFuture = new MySqlCommand("DELETE FROM finance_payables WHERE recurring_id=@id AND status='PENDENTE' AND due_date > CURDATE()", connection, (MySqlTransaction)tx))
        {
            deleteFuture.Parameters.AddWithValue("@id", recurringId);
            await deleteFuture.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    private static int? NullableInt(MySqlDataReader reader, string column)
    {
        var ord = reader.GetOrdinal(column);
        return reader.IsDBNull(ord) ? null : reader.GetInt32(column);
    }

    private static string? NullableString(MySqlDataReader reader, string column)
    {
        var ord = reader.GetOrdinal(column);
        return reader.IsDBNull(ord) ? null : reader.GetString(column);
    }
}
