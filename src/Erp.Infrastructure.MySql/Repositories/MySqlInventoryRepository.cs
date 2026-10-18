using Erp.Infrastructure.MySql.Data;
using Erp.Infrastructure.MySql.Utilities;
using Erp.Modules.Inventory;
using MySqlConnector;

namespace Erp.Infrastructure.MySql.Repositories;

public sealed class MySqlInventoryRepository : IInventoryRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlInventoryRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<InventoryProductBalance>> GetProductBalancesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id, sku, name, stock_quantity FROM sales_products ORDER BY sku";
        var list = new List<InventoryProductBalance>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await r.ReadAsync(cancellationToken))
        {
            list.Add(new InventoryProductBalance
            {
                ProductId = r.GetInt32("id"),
                Sku = r.GetString("sku"),
                Name = r.GetString("name"),
                Quantity = r.GetDecimal("stock_quantity")
            });
        }
        return list;
    }

    public async Task<IReadOnlyList<Supplier>> GetSuppliersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id, code, name, tax_id, is_active, contact_name, email, phone, street, number, district, city, state, zip_code, minimum_order, segment FROM inventory_suppliers ORDER BY name";
        var list = new List<Supplier>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await r.ReadAsync(cancellationToken))
        {
            list.Add(new Supplier
            {
                Id = r.GetInt32("id"),
                Code = r.GetString("code"),
                Name = r.GetString("name"),
                TaxId = r.GetString("tax_id"),
                IsActive = r.GetBoolean("is_active"),
                ContactName = NullableString(r, "contact_name"),
                Email = NullableString(r, "email"),
                Phone = NullableString(r, "phone"),
                Street = NullableString(r, "street"),
                Number = NullableString(r, "number"),
                District = NullableString(r, "district"),
                City = NullableString(r, "city"),
                State = NullableString(r, "state"),
                ZipCode = NullableString(r, "zip_code"),
                MinimumOrder = r.GetDecimal("minimum_order"),
                Segment = NullableString(r, "segment")
            });
        }
        return list;
    }

    public async Task<IReadOnlyList<Supplier>> GetSupplierCatalogItemsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id, code, name, tax_id, segment, minimum_order, city, state, is_active FROM inventory_suppliers ORDER BY name";
        var list = new List<Supplier>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await r.ReadAsync(cancellationToken))
        {
            list.Add(new Supplier
            {
                Id = r.GetInt32("id"),
                Code = r.GetString("code"),
                Name = r.GetString("name"),
                TaxId = r.GetString("tax_id"),
                IsActive = r.GetBoolean("is_active"),
                Segment = NullableString(r, "segment"),
                MinimumOrder = r.GetDecimal("minimum_order"),
                City = NullableString(r, "city"),
                State = NullableString(r, "state")
            });
        }
        return list;
    }

    public async Task<Supplier?> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id, code, name, tax_id, is_active, contact_name, email, phone, street, number, district, city, state, zip_code, minimum_order, segment FROM inventory_suppliers WHERE id=@id";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", id);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await r.ReadAsync(cancellationToken))
            return null;

        return new Supplier
        {
            Id = r.GetInt32("id"),
            Code = r.GetString("code"),
            Name = r.GetString("name"),
            TaxId = r.GetString("tax_id"),
            IsActive = r.GetBoolean("is_active"),
            ContactName = NullableString(r, "contact_name"),
            Email = NullableString(r, "email"),
            Phone = NullableString(r, "phone"),
            Street = NullableString(r, "street"),
            Number = NullableString(r, "number"),
            District = NullableString(r, "district"),
            City = NullableString(r, "city"),
            State = NullableString(r, "state"),
            ZipCode = NullableString(r, "zip_code"),
            MinimumOrder = r.GetDecimal("minimum_order"),
            Segment = NullableString(r, "segment")
        };
    }

    public async Task<string> GetNextSupplierCodeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT code FROM inventory_suppliers WHERE code REGEXP '^F[0-9]+$' ORDER BY CAST(SUBSTRING(code, 2) AS UNSIGNED) DESC LIMIT 1";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        var current = (await command.ExecuteScalarAsync(cancellationToken))?.ToString();

        if (string.IsNullOrWhiteSpace(current) || current.Length <= 1 || !int.TryParse(current[1..], out var number))
            return "F001";

        return $"F{number + 1:D3}";
    }

    public async Task<int> AddSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        const string sql = @"INSERT INTO inventory_suppliers (code, name, tax_id, is_active, contact_name, email, phone, street, number, district, city, state, zip_code, minimum_order, segment)
VALUES (@code, @name, @taxId, @isActive, @contactName, @email, @phone, @street, @number, @district, @city, @state, @zipCode, @minimumOrder, @segment); SELECT LAST_INSERT_ID();";

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var code = attempt == 0 && !string.IsNullOrWhiteSpace(supplier.Code)
                ? supplier.Code.Trim().ToUpperInvariant()
                : await GetNextSupplierCodeAsync(cancellationToken);

            await using var connection = _connectionFactory.Create();
            await connection.OpenAsync(cancellationToken);
            await using var cmd = new MySqlCommand(sql, connection);
            FillSupplier(cmd, supplier, code);

            try
            {
                return Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
            }
            catch (MySqlException ex) when (ShouldRetryGeneratedCode(ex, "uq_inventory_suppliers_code"))
            {
            }
            catch (MySqlException ex)
            {
                throw TranslateWriteException(ex);
            }
        }

        throw new InvalidOperationException("Nao foi possivel gerar um codigo unico para o fornecedor.");
    }

    public async Task UpdateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        const string sql = @"UPDATE inventory_suppliers SET code=@code, name=@name, tax_id=@taxId, is_active=@isActive, contact_name=@contactName, email=@email, phone=@phone, street=@street, number=@number, district=@district, city=@city, state=@state, zip_code=@zipCode, minimum_order=@minimumOrder, segment=@segment WHERE id=@id";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        FillSupplier(cmd, supplier);
        cmd.Parameters.AddWithValue("@id", supplier.Id);
        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException ex)
        {
            throw TranslateWriteException(ex);
        }
    }

    public async Task<int> CreatePurchaseRequestAsync(int productId, decimal quantity, string? notes, CancellationToken cancellationToken = default)
    {
        const string sql = @"INSERT INTO purchase_requests (product_id, requested_qty, status, notes)
VALUES (@productId, @qty, 'ABERTA', @notes); SELECT LAST_INSERT_ID();";

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@productId", productId);
        cmd.Parameters.AddWithValue("@qty", quantity);
        cmd.Parameters.AddWithValue("@notes", notes);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<PurchaseRequestSummary>> GetPurchaseRequestsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT pr.id, p.sku product_sku, pr.requested_qty, pr.status, s.name supplier_name, pr.approved_qty, pr.approved_unit_price, pr.notes, pr.created_at
FROM purchase_requests pr
JOIN sales_products p ON p.id = pr.product_id
LEFT JOIN inventory_suppliers s ON s.id = pr.supplier_id
ORDER BY pr.id DESC";

        var list = new List<PurchaseRequestSummary>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await r.ReadAsync(cancellationToken))
        {
            list.Add(new PurchaseRequestSummary
            {
                Id = r.GetInt32("id"),
                ProductSku = r.GetString("product_sku"),
                RequestedQuantity = r.GetDecimal("requested_qty"),
                Status = r.GetString("status"),
                SupplierName = NullableString(r, "supplier_name"),
                ApprovedQuantity = NullableDecimal(r, "approved_qty"),
                ApprovedUnitPrice = NullableDecimal(r, "approved_unit_price"),
                Notes = NullableString(r, "notes"),
                CreatedAt = r.GetDateTime("created_at")
            });
        }
        return list;
    }

    public async Task RejectPurchaseRequestAsync(int requestId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE purchase_requests SET status='RECUSADA' WHERE id=@id AND status='ABERTA'";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", requestId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> ApprovePurchaseRequestAsync(int requestId, int supplierId, decimal approvedQty, decimal unitPrice, string paymentTerm, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        const string lockSql = "SELECT product_id, status FROM purchase_requests WHERE id=@id FOR UPDATE";
        int productId;
        string status;
        await using (var lockCmd = new MySqlCommand(lockSql, connection, (MySqlTransaction)tx))
        {
            lockCmd.Parameters.AddWithValue("@id", requestId);
            await using var r = await lockCmd.ExecuteReaderAsync(cancellationToken);
            if (!await r.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Solicitacao nao encontrada.");
            }

            productId = r.GetInt32("product_id");
            status = r.GetString("status");
        }

        if (!string.Equals(status, "ABERTA", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Somente solicitacoes abertas podem ser aprovadas.");
        }

        const string updateReqSql = @"UPDATE purchase_requests
SET status='APROVADA', supplier_id=@supplierId, approved_qty=@qty, approved_unit_price=@price, approved_at=NOW()
WHERE id=@id";

        await using (var upd = new MySqlCommand(updateReqSql, connection, (MySqlTransaction)tx))
        {
            upd.Parameters.AddWithValue("@supplierId", supplierId);
            upd.Parameters.AddWithValue("@qty", approvedQty);
            upd.Parameters.AddWithValue("@price", unitPrice);
            upd.Parameters.AddWithValue("@id", requestId);
            await upd.ExecuteNonQueryAsync(cancellationToken);
        }

        const string insertPurchaseSql = @"INSERT INTO purchases (request_id, supplier_id, product_id, quantity, unit_price, payment_term, source_type, status)
VALUES (@requestId, @supplierId, @productId, @qty, @price, @paymentTerm, 'SOLICITACAO', 'PENDENTE_ENTREGA'); SELECT LAST_INSERT_ID();";

        int purchaseId;
        await using (var ins = new MySqlCommand(insertPurchaseSql, connection, (MySqlTransaction)tx))
        {
            ins.Parameters.AddWithValue("@requestId", requestId);
            ins.Parameters.AddWithValue("@supplierId", supplierId);
            ins.Parameters.AddWithValue("@productId", productId);
            ins.Parameters.AddWithValue("@qty", approvedQty);
            ins.Parameters.AddWithValue("@price", unitPrice);
            ins.Parameters.AddWithValue("@paymentTerm", string.IsNullOrWhiteSpace(paymentTerm) ? "A_VISTA" : paymentTerm.Trim());
            purchaseId = Convert.ToInt32(await ins.ExecuteScalarAsync(cancellationToken));
        }

        await CreatePayablesForPurchaseAsync(connection, (MySqlTransaction)tx, purchaseId, supplierId, approvedQty * unitPrice, paymentTerm, "Compra de produtos", cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return purchaseId;
    }

    public async Task<int> CreateDirectPurchaseAsync(int productId, int supplierId, decimal quantity, decimal unitPrice, string paymentTerm, string? notes, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        const string insertPurchaseSql = @"
INSERT INTO purchases (request_id, supplier_id, product_id, quantity, unit_price, payment_term, source_type, status)
VALUES (NULL, @supplierId, @productId, @qty, @price, @paymentTerm, 'DIRETA', 'PENDENTE_ENTREGA');
SELECT LAST_INSERT_ID();";

        int purchaseId;
        await using (var purchase = new MySqlCommand(insertPurchaseSql, connection, (MySqlTransaction)tx))
        {
            purchase.Parameters.AddWithValue("@supplierId", supplierId);
            purchase.Parameters.AddWithValue("@productId", productId);
            purchase.Parameters.AddWithValue("@qty", quantity);
            purchase.Parameters.AddWithValue("@price", unitPrice);
            purchase.Parameters.AddWithValue("@paymentTerm", string.IsNullOrWhiteSpace(paymentTerm) ? "A_VISTA" : paymentTerm.Trim());
            purchaseId = Convert.ToInt32(await purchase.ExecuteScalarAsync(cancellationToken));
        }

        await CreatePayablesForPurchaseAsync(connection, (MySqlTransaction)tx, purchaseId, supplierId, quantity * unitPrice, paymentTerm, string.IsNullOrWhiteSpace(notes) ? "Compra direta" : $"Compra direta - {notes!.Trim()}", cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return purchaseId;
    }

    public async Task<IReadOnlyList<PurchaseSummary>> GetPurchasesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT p.id, p.request_id, p.source_type, s.name supplier_name, prd.sku product_sku, p.quantity, p.unit_price, p.status, p.created_at
FROM purchases p
JOIN inventory_suppliers s ON s.id = p.supplier_id
JOIN sales_products prd ON prd.id = p.product_id
ORDER BY p.id DESC";

        var list = new List<PurchaseSummary>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await r.ReadAsync(cancellationToken))
        {
            list.Add(new PurchaseSummary
            {
                Id = r.GetInt32("id"),
                RequestId = NullableInt(r, "request_id"),
                SourceType = r.GetString("source_type"),
                SupplierName = r.GetString("supplier_name"),
                ProductSku = r.GetString("product_sku"),
                Quantity = r.GetDecimal("quantity"),
                UnitPrice = r.GetDecimal("unit_price"),
                Status = r.GetString("status"),
                CreatedAt = r.GetDateTime("created_at")
            });
        }

        return list;
    }

    public async Task MarkPurchaseDeliveredAsync(int purchaseId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        const string lockSql = "SELECT product_id, quantity, status FROM purchases WHERE id=@id FOR UPDATE";
        int productId;
        decimal qty;
        string status;
        await using (var lockCmd = new MySqlCommand(lockSql, connection, (MySqlTransaction)tx))
        {
            lockCmd.Parameters.AddWithValue("@id", purchaseId);
            await using var r = await lockCmd.ExecuteReaderAsync(cancellationToken);
            if (!await r.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Compra nao encontrada.");
            }

            productId = r.GetInt32("product_id");
            qty = r.GetDecimal("quantity");
            status = r.GetString("status");
        }

        if (!string.Equals(status, "PENDENTE_ENTREGA", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Apenas compras pendentes podem ser marcadas como entregues.");
        }

        await using (var updPurchase = new MySqlCommand("UPDATE purchases SET status='ENTREGUE', delivered_at=NOW() WHERE id=@id", connection, (MySqlTransaction)tx))
        {
            updPurchase.Parameters.AddWithValue("@id", purchaseId);
            await updPurchase.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var updStock = new MySqlCommand("UPDATE sales_products SET stock_quantity = stock_quantity + @qty WHERE id=@productId", connection, (MySqlTransaction)tx))
        {
            updStock.Parameters.AddWithValue("@qty", qty);
            updStock.Parameters.AddWithValue("@productId", productId);
            await updStock.ExecuteNonQueryAsync(cancellationToken);
        }

        const string movementSql = @"INSERT INTO stock_movements (product_id, movement_type, quantity, source_type, source_id, notes)
VALUES (@productId, 'ENTRADA', @qty, 'COMPRA', @sourceId, 'Entrada por compra entregue');";
        await using (var move = new MySqlCommand(movementSql, connection, (MySqlTransaction)tx))
        {
            move.Parameters.AddWithValue("@productId", productId);
            move.Parameters.AddWithValue("@qty", qty);
            move.Parameters.AddWithValue("@sourceId", purchaseId);
            await move.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    public async Task CancelPurchaseAsync(int purchaseId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE purchases SET status='CANCELADA' WHERE id=@id AND status='PENDENTE_ENTREGA'";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", purchaseId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovementSummary>> GetStockEntriesAsync(CancellationToken cancellationToken = default)
        => await GetMovementsAsync("ENTRADA", cancellationToken);

    public async Task<IReadOnlyList<StockMovementSummary>> GetStockOutputsAsync(CancellationToken cancellationToken = default)
        => await GetMovementsAsync("SAIDA", cancellationToken);

    public async Task<int> CreateStockEntryAsync(int productId, decimal quantity, string? notes, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) throw new InvalidOperationException("A quantidade deve ser maior que zero.");

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        await using (var stock = new MySqlCommand("UPDATE sales_products SET stock_quantity = stock_quantity + @qty WHERE id=@productId", connection, (MySqlTransaction)tx))
        {
            stock.Parameters.AddWithValue("@qty", quantity);
            stock.Parameters.AddWithValue("@productId", productId);
            var affected = await stock.ExecuteNonQueryAsync(cancellationToken);
            if (affected <= 0) throw new InvalidOperationException("Produto nao encontrado.");
        }

        const string movementSql = @"INSERT INTO stock_movements (product_id, movement_type, quantity, source_type, source_id, notes)
VALUES (@productId, 'ENTRADA', @qty, 'MANUAL', NULL, @notes); SELECT LAST_INSERT_ID();";
        int movementId;
        await using (var move = new MySqlCommand(movementSql, connection, (MySqlTransaction)tx))
        {
            move.Parameters.AddWithValue("@productId", productId);
            move.Parameters.AddWithValue("@qty", quantity);
            move.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(notes) ? "Entrada manual" : notes.Trim());
            movementId = Convert.ToInt32(await move.ExecuteScalarAsync(cancellationToken));
        }

        await tx.CommitAsync(cancellationToken);
        return movementId;
    }

    public async Task<int> CreateStockOutputAsync(int productId, decimal quantity, string? notes, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) throw new InvalidOperationException("A quantidade deve ser maior que zero.");

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        await using (var stock = new MySqlCommand("UPDATE sales_products SET stock_quantity = stock_quantity - @qty WHERE id=@productId AND stock_quantity >= @qty", connection, (MySqlTransaction)tx))
        {
            stock.Parameters.AddWithValue("@qty", quantity);
            stock.Parameters.AddWithValue("@productId", productId);
            var affected = await stock.ExecuteNonQueryAsync(cancellationToken);
            if (affected <= 0) throw new InvalidOperationException("Saldo de estoque insuficiente ou produto nao encontrado.");
        }

        const string movementSql = @"INSERT INTO stock_movements (product_id, movement_type, quantity, source_type, source_id, notes)
VALUES (@productId, 'SAIDA', @qty, 'MANUAL', NULL, @notes); SELECT LAST_INSERT_ID();";
        int movementId;
        await using (var move = new MySqlCommand(movementSql, connection, (MySqlTransaction)tx))
        {
            move.Parameters.AddWithValue("@productId", productId);
            move.Parameters.AddWithValue("@qty", quantity);
            move.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(notes) ? "Saida manual" : notes.Trim());
            movementId = Convert.ToInt32(await move.ExecuteScalarAsync(cancellationToken));
        }

        await tx.CommitAsync(cancellationToken);
        return movementId;
    }

    private async Task<IReadOnlyList<StockMovementSummary>> GetMovementsAsync(string type, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT m.id, m.movement_type, p.sku product_sku, m.quantity, m.source_type, m.notes, m.created_at
FROM stock_movements m
JOIN sales_products p ON p.id = m.product_id
WHERE m.movement_type = @type
ORDER BY m.id DESC";

        var list = new List<StockMovementSummary>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@type", type);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await r.ReadAsync(cancellationToken))
        {
            list.Add(new StockMovementSummary
            {
                Id = r.GetInt32("id"),
                MovementType = r.GetString("movement_type"),
                ProductSku = r.GetString("product_sku"),
                Quantity = r.GetDecimal("quantity"),
                SourceType = r.GetString("source_type"),
                Notes = NullableString(r, "notes"),
                CreatedAt = r.GetDateTime("created_at")
            });
        }

        return list;
    }

    private static async Task CreatePayablesForPurchaseAsync(MySqlConnection connection, MySqlTransaction tx, int purchaseId, int supplierId, decimal totalAmount, string paymentTerm, string description, CancellationToken ct)
    {
        var normalizedTerm = string.IsNullOrWhiteSpace(paymentTerm) ? "A_VISTA" : paymentTerm.Trim();
        var installments = PaymentInstallmentCalculator.BuildInstallments(totalAmount, DateTime.Today, normalizedTerm);
        const string sql = @"
INSERT INTO finance_payables (purchase_id, supplier_id, description, installment_number, due_date, amount, payment_term, status)
VALUES (@purchaseId, @supplierId, @description, @installment, @dueDate, @amount, @paymentTerm, 'PENDENTE');";

        foreach (var installment in installments)
        {
            await using var cmd = new MySqlCommand(sql, connection, tx);
            cmd.Parameters.AddWithValue("@purchaseId", purchaseId);
            cmd.Parameters.AddWithValue("@supplierId", supplierId);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@installment", installment.Number);
            cmd.Parameters.AddWithValue("@dueDate", installment.DueDate);
            cmd.Parameters.AddWithValue("@amount", installment.Amount);
            cmd.Parameters.AddWithValue("@paymentTerm", normalizedTerm);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    private static void FillSupplier(MySqlCommand cmd, Supplier s, string? codeOverride = null)
    {
        cmd.Parameters.AddWithValue("@code", (codeOverride ?? s.Code).Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("@name", s.Name);
        cmd.Parameters.AddWithValue("@taxId", s.TaxId);
        cmd.Parameters.AddWithValue("@isActive", s.IsActive);
        cmd.Parameters.AddWithValue("@contactName", (object?)s.ContactName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@email", (object?)s.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@phone", (object?)s.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@street", (object?)s.Street ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@number", (object?)s.Number ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@district", (object?)s.District ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@city", (object?)s.City ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@state", (object?)s.State ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@zipCode", (object?)s.ZipCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@minimumOrder", s.MinimumOrder);
        cmd.Parameters.AddWithValue("@segment", (object?)s.Segment ?? DBNull.Value);
    }

    private static string? NullableString(MySqlDataReader reader, string column)
    {
        var ord = reader.GetOrdinal(column);
        return reader.IsDBNull(ord) ? null : reader.GetString(column);
    }

    private static decimal? NullableDecimal(MySqlDataReader reader, string column)
    {
        var ord = reader.GetOrdinal(column);
        return reader.IsDBNull(ord) ? null : reader.GetDecimal(column);
    }

    private static int? NullableInt(MySqlDataReader reader, string column)
    {
        var ord = reader.GetOrdinal(column);
        return reader.IsDBNull(ord) ? null : reader.GetInt32(column);
    }

    private static bool ShouldRetryGeneratedCode(MySqlException ex, string indexName)
        => ex.Number == 1062 && ex.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase);

    private static Exception TranslateWriteException(MySqlException ex)
    {
        if (ex.Number != 1062)
            return ex;

        return ex.Message switch
        {
            var m when m.Contains("uq_inventory_suppliers_code", StringComparison.OrdinalIgnoreCase)
                => new InvalidOperationException("Ja existe um fornecedor com este codigo."),
            var m when m.Contains("uq_inventory_suppliers_tax_id", StringComparison.OrdinalIgnoreCase)
                => new InvalidOperationException("Ja existe um fornecedor com este CNPJ."),
            _ => new InvalidOperationException("Ja existe um fornecedor com um identificador igual.")
        };
    }
}
