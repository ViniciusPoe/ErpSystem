using Erp.Infrastructure.MySql.Data;
using Erp.Infrastructure.MySql.Utilities;
using Erp.Modules.Sales;
using MySqlConnector;

namespace Erp.Infrastructure.MySql.Repositories;

public sealed class MySqlSalesRepository : ISalesRepository
{
    private readonly MySqlConnectionFactory _connectionFactory;
    private readonly MySqlSchemaInitializer _schemaInitializer;

    public MySqlSalesRepository(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        _schemaInitializer = new MySqlSchemaInitializer(connectionFactory);
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => _schemaInitializer.InitializeAsync(cancellationToken);

    public async Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT id, code, name, document, is_active, contact_name, email, phone, street, number, district, city, state, zip_code FROM sales_customers ORDER BY name";
        var items = new List<Customer>();

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new Customer
            {
                Id = reader.GetInt32("id"),
                Code = reader.GetString("code"),
                Name = reader.GetString("name"),
                Document = reader.GetString("document"),
                IsActive = reader.GetBoolean("is_active"),
                ContactName = NullableString(reader, "contact_name"),
                Email = NullableString(reader, "email"),
                Phone = NullableString(reader, "phone"),
                Street = NullableString(reader, "street"),
                Number = NullableString(reader, "number"),
                District = NullableString(reader, "district"),
                City = NullableString(reader, "city"),
                State = NullableString(reader, "state"),
                ZipCode = NullableString(reader, "zip_code")
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<Customer>> GetCustomerCatalogItemsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT id, code, name, document, city, state, is_active FROM sales_customers ORDER BY name";
        var items = new List<Customer>();

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new Customer
            {
                Id = reader.GetInt32("id"),
                Code = reader.GetString("code"),
                Name = reader.GetString("name"),
                Document = reader.GetString("document"),
                IsActive = reader.GetBoolean("is_active"),
                City = NullableString(reader, "city"),
                State = NullableString(reader, "state")
            });
        }

        return items;
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT id, code, name, document, is_active, contact_name, email, phone, street, number, district, city, state, zip_code FROM sales_customers WHERE id=@id";

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new Customer
        {
            Id = reader.GetInt32("id"),
            Code = reader.GetString("code"),
            Name = reader.GetString("name"),
            Document = reader.GetString("document"),
            IsActive = reader.GetBoolean("is_active"),
            ContactName = NullableString(reader, "contact_name"),
            Email = NullableString(reader, "email"),
            Phone = NullableString(reader, "phone"),
            Street = NullableString(reader, "street"),
            Number = NullableString(reader, "number"),
            District = NullableString(reader, "district"),
            City = NullableString(reader, "city"),
            State = NullableString(reader, "state"),
            ZipCode = NullableString(reader, "zip_code")
        };
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT p.id, p.code, p.sku, p.name, p.description, p.barcode, p.unit_price, p.cost_price, p.stock_quantity,
       p.image_path, p.color, p.sub_classification_id, p.ncm, p.weight, p.width, p.height, p.depth, p.voltage, p.is_active,
       sc.name sub_class_name, sc.code sub_class_code, c.name class_name, c.code class_code
FROM sales_products p
LEFT JOIN product_sub_classifications sc ON sc.id = p.sub_classification_id
LEFT JOIN product_classifications c ON c.id = sc.classification_id
ORDER BY p.sku";
        var items = new List<Product>();

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new Product
            {
                Id = reader.GetInt32("id"),
                Code = NullableString(reader, "code"),
                Sku = reader.GetString("sku"),
                Name = reader.GetString("name"),
                IsActive = reader.GetBoolean("is_active"),
                Description = NullableString(reader, "description"),
                Barcode = NullableString(reader, "barcode"),
                UnitPrice = reader.GetDecimal("unit_price"),
                CostPrice = reader.GetDecimal("cost_price"),
                Quantity = reader.GetDecimal("stock_quantity"),
                ImagePath = NullableString(reader, "image_path"),
                Color = NullableString(reader, "color"),
                SubClassificationId = reader.IsDBNull(reader.GetOrdinal("sub_classification_id")) ? null : reader.GetInt32("sub_classification_id"),
                Ncm = NullableString(reader, "ncm"),
                Weight = reader.GetDecimal("weight"),
                Width = reader.GetDecimal("width"),
                Height = reader.GetDecimal("height"),
                Depth = reader.GetDecimal("depth"),
                Voltage = NullableString(reader, "voltage"),
                ClassificationDisplay = NullableString(reader, "class_name") is string cn ? $"{reader.GetString("class_code")} - {cn}" : null,
                SubClassificationDisplay = NullableString(reader, "sub_class_name") is string sn ? $"{reader.GetString("sub_class_code")} - {sn}" : null
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<Product>> GetProductCatalogItemsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT p.id, p.code, p.sku, p.name, p.barcode, p.unit_price, p.color, p.is_active, p.brand_id,
       sc.name sub_class_name, sc.code sub_class_code, c.name class_name, c.code class_code,
       b.name brand_name
FROM sales_products p
LEFT JOIN product_sub_classifications sc ON sc.id = p.sub_classification_id
LEFT JOIN product_classifications c ON c.id = sc.classification_id
LEFT JOIN product_brands b ON b.id = p.brand_id
ORDER BY p.sku";
        var items = new List<Product>();

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var classCode = NullableString(reader, "class_code");
            var className = NullableString(reader, "class_name");
            var subCode = NullableString(reader, "sub_class_code");
            var subName = NullableString(reader, "sub_class_name");

            items.Add(new Product
            {
                Id = reader.GetInt32("id"),
                Code = NullableString(reader, "code"),
                Sku = reader.GetString("sku"),
                Name = reader.GetString("name"),
                Barcode = NullableString(reader, "barcode"),
                IsActive = reader.GetBoolean("is_active"),
                UnitPrice = reader.GetDecimal("unit_price"),
                Quantity = 0,
                Color = NullableString(reader, "color"),
                BrandId = reader.IsDBNull(reader.GetOrdinal("brand_id")) ? null : reader.GetInt32("brand_id"),
                BrandDisplay = NullableString(reader, "brand_name"),
                ClassificationDisplay = classCode is null || className is null ? null : $"{classCode} - {className}",
                SubClassificationDisplay = subCode is null || subName is null ? null : $"{subCode} - {subName}"
            });
        }

        return items;
    }

    public async Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT p.id, p.code, p.sku, p.name, p.description, p.barcode, p.unit_price, p.cost_price, p.stock_quantity,
       p.image_path, p.color, p.sub_classification_id, p.ncm, p.weight, p.width, p.height, p.depth, p.voltage, p.is_active, p.brand_id,
       sc.name sub_class_name, sc.code sub_class_code, c.name class_name, c.code class_code,
       b.name brand_name
FROM sales_products p
LEFT JOIN product_sub_classifications sc ON sc.id = p.sub_classification_id
LEFT JOIN product_classifications c ON c.id = sc.classification_id
LEFT JOIN product_brands b ON b.id = p.brand_id
WHERE p.id = @id";

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var classCode = NullableString(reader, "class_code");
        var className = NullableString(reader, "class_name");
        var subCode = NullableString(reader, "sub_class_code");
        var subName = NullableString(reader, "sub_class_name");

        return new Product
        {
            Id = reader.GetInt32("id"),
            Code = NullableString(reader, "code"),
            Sku = reader.GetString("sku"),
            Name = reader.GetString("name"),
            IsActive = reader.GetBoolean("is_active"),
            Description = NullableString(reader, "description"),
            Barcode = NullableString(reader, "barcode"),
            UnitPrice = reader.GetDecimal("unit_price"),
            CostPrice = reader.GetDecimal("cost_price"),
            Quantity = reader.GetDecimal("stock_quantity"),
            ImagePath = NullableString(reader, "image_path"),
            Color = NullableString(reader, "color"),
            SubClassificationId = reader.IsDBNull(reader.GetOrdinal("sub_classification_id")) ? null : reader.GetInt32("sub_classification_id"),
            Ncm = NullableString(reader, "ncm"),
            Weight = reader.GetDecimal("weight"),
            Width = reader.GetDecimal("width"),
            Height = reader.GetDecimal("height"),
            Depth = reader.GetDecimal("depth"),
            Voltage = NullableString(reader, "voltage"),
            BrandId = reader.IsDBNull(reader.GetOrdinal("brand_id")) ? null : reader.GetInt32("brand_id"),
            BrandDisplay = NullableString(reader, "brand_name"),
            ClassificationDisplay = classCode is null || className is null ? null : $"{classCode} - {className}",
            SubClassificationDisplay = subCode is null || subName is null ? null : $"{subCode} - {subName}"
        };
    }

    public async Task<string> GetNextCustomerCodeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT code FROM sales_customers WHERE code REGEXP '^C[0-9]+$' ORDER BY CAST(SUBSTRING(code, 2) AS UNSIGNED) DESC LIMIT 1";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        var current = (await command.ExecuteScalarAsync(cancellationToken))?.ToString();

        if (string.IsNullOrWhiteSpace(current) || current.Length <= 1 || !int.TryParse(current[1..], out var number))
            return "C0001";

        return $"C{number + 1:D4}";
    }

    public async Task<string> GetNextProductCodeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT code FROM sales_products WHERE code REGEXP '^P[0-9]+$' ORDER BY CAST(SUBSTRING(code, 2) AS UNSIGNED) DESC LIMIT 1";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        var current = (await command.ExecuteScalarAsync(cancellationToken))?.ToString();

        if (string.IsNullOrWhiteSpace(current) || current.Length <= 1 || !int.TryParse(current[1..], out var number))
            return "P0001";

        return $"P{number + 1:D4}";
    }

    public async Task<IReadOnlyList<Carrier>> GetCarriersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id, code, name, document, contact_name, email, phone, street, number, district, city, state, zip_code FROM sales_carriers ORDER BY name";
        var items = new List<Carrier>();

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new Carrier
            {
                Id = reader.GetInt32("id"),
                Code = reader.GetString("code"),
                Name = reader.GetString("name"),
                Document = NullableString(reader, "document") ?? string.Empty,
                ContactName = NullableString(reader, "contact_name"),
                Email = NullableString(reader, "email"),
                Phone = NullableString(reader, "phone"),
                Street = NullableString(reader, "street"),
                Number = NullableString(reader, "number"),
                District = NullableString(reader, "district"),
                City = NullableString(reader, "city"),
                State = NullableString(reader, "state"),
                ZipCode = NullableString(reader, "zip_code")
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<Municipality>> GetMunicipalitiesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id, code, name, state, country FROM municipalities ORDER BY country, state, name";
        var items = new List<Municipality>();

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new Municipality
            {
                Id = reader.GetInt32("id"),
                Code = reader.GetString("code"),
                Name = reader.GetString("name"),
                State = reader.GetString("state"),
                Country = reader.IsDBNull(reader.GetOrdinal("country")) ? "Brasil" : reader.GetString("country")
            });
        }

        return items;
    }

    public async Task<int> AddMunicipalityAsync(Municipality municipality, CancellationToken cancellationToken = default)
    {
        const string sql = @"INSERT INTO municipalities (code, name, state, country) VALUES (@code, @name, @state, @country); SELECT LAST_INSERT_ID();";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@code", municipality.Code);
        command.Parameters.AddWithValue("@name", municipality.Name);
        command.Parameters.AddWithValue("@state", municipality.State);
        command.Parameters.AddWithValue("@country", municipality.Country);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task UpdateMunicipalityAsync(Municipality municipality, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE municipalities SET code=@code, name=@name, state=@state, country=@country WHERE id=@id";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@code", municipality.Code);
        command.Parameters.AddWithValue("@name", municipality.Name);
        command.Parameters.AddWithValue("@state", municipality.State);
        command.Parameters.AddWithValue("@country", municipality.Country);
        command.Parameters.AddWithValue("@id", municipality.Id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteMunicipalityAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM municipalities WHERE id=@id";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        const string sql = @"INSERT INTO sales_customers (code, name, document, is_active, contact_name, email, phone, street, number, district, city, state, zip_code) VALUES (@code,@name,@document,@isActive,@contactName,@email,@phone,@street,@number,@district,@city,@state,@zipCode); SELECT LAST_INSERT_ID();";

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var code = attempt == 0 && !string.IsNullOrWhiteSpace(customer.Code)
                ? customer.Code.Trim().ToUpperInvariant()
                : await GetNextCustomerCodeAsync(cancellationToken);

            await using var connection = _connectionFactory.Create();
            await connection.OpenAsync(cancellationToken);
            await using var command = new MySqlCommand(sql, connection);
            FillCustomer(command, customer, code);

            try
            {
                return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            }
            catch (MySqlException ex) when (ShouldRetryGeneratedCode(ex, "uq_sales_customers_code"))
            {
            }
            catch (MySqlException ex)
            {
                throw TranslateWriteException(ex);
            }
        }

        throw new InvalidOperationException("Nao foi possivel gerar um codigo unico para o cliente.");
    }

    public async Task UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        const string sql = @"UPDATE sales_customers SET code=@code,name=@name,document=@document,is_active=@isActive,contact_name=@contactName,email=@email,phone=@phone,street=@street,number=@number,district=@district,city=@city,state=@state,zip_code=@zipCode WHERE id=@id";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        FillCustomer(command, customer);
        command.Parameters.AddWithValue("@id", customer.Id);
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException ex)
        {
            throw TranslateWriteException(ex);
        }
    }

    public async Task<int> AddProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        return await SaveProductAsync(product, [], cancellationToken);
    }

    public async Task UpdateProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        const string sql = @"UPDATE sales_products SET code=@code,sku=@sku,name=@name,is_active=@isActive,description=@description,barcode=@barcode,unit_price=@unitPrice,cost_price=@costPrice,tax_rate=0,image_path=@imagePath,color=@color,sub_classification_id=@subClassId,ncm=@ncm,weight=@weight,width=@width,height=@height,depth=@depth,voltage=@voltage,brand_id=@brandId WHERE id=@id";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        FillProduct(command, product);
        command.Parameters.AddWithValue("@id", product.Id);
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException ex)
        {
            throw TranslateWriteException(ex);
        }
    }

    public async Task<int> SaveProductAsync(Product product, IReadOnlyList<(int FieldId, string Value)> values, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var code = NormalizeProductCode(product.Code);
            if (product.Id <= 0 && (attempt > 0 || string.IsNullOrWhiteSpace(code)))
            {
                code = await GetNextProductCodeAsync(cancellationToken);
            }

            await using var connection = _connectionFactory.Create();
            await connection.OpenAsync(cancellationToken);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                int productId;
                if (product.Id > 0)
                {
                    const string updateSql = @"UPDATE sales_products SET code=@code,sku=@sku,name=@name,is_active=@isActive,description=@description,barcode=@barcode,unit_price=@unitPrice,cost_price=@costPrice,tax_rate=0,image_path=@imagePath,color=@color,sub_classification_id=@subClassId,ncm=@ncm,weight=@weight,width=@width,height=@height,depth=@depth,voltage=@voltage,brand_id=@brandId WHERE id=@id";
                    await using var update = new MySqlCommand(updateSql, connection, (MySqlTransaction)tx);
                    FillProduct(update, product, code);
                    update.Parameters.AddWithValue("@id", product.Id);
                    await update.ExecuteNonQueryAsync(cancellationToken);
                    productId = product.Id;
                }
                else
                {
                    const string insertSql = @"INSERT INTO sales_products (code,sku,name,is_active,description,barcode,unit_price,cost_price,tax_rate,stock_quantity,image_path,color,sub_classification_id,ncm,weight,width,height,depth,voltage,brand_id) VALUES (@code,@sku,@name,@isActive,@description,@barcode,@unitPrice,@costPrice,0,@stockQty,@imagePath,@color,@subClassId,@ncm,@weight,@width,@height,@depth,@voltage,@brandId); SELECT LAST_INSERT_ID();";
                    await using var insert = new MySqlCommand(insertSql, connection, (MySqlTransaction)tx);
                    FillProduct(insert, product, code);
                    productId = Convert.ToInt32(await insert.ExecuteScalarAsync(cancellationToken));
                }

                await ReplaceProductCustomValuesAsync(connection, (MySqlTransaction)tx, productId, values, cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return productId;
            }
            catch (MySqlException ex) when (product.Id <= 0 && ShouldRetryGeneratedCode(ex, "uq_sales_products_code"))
            {
                await tx.RollbackAsync(cancellationToken);
            }
            catch (MySqlException ex)
            {
                await tx.RollbackAsync(cancellationToken);
                throw TranslateWriteException(ex);
            }
        }

        throw new InvalidOperationException("Nao foi possivel gerar um codigo unico para o produto.");
    }

    public async Task<int> AddCarrierAsync(Carrier carrier, CancellationToken cancellationToken = default)
    {
        const string sql = @"INSERT INTO sales_carriers (code,name,document,contact_name,email,phone,street,number,district,city,state,zip_code) VALUES (@code,@name,@document,@contactName,@email,@phone,@street,@number,@district,@city,@state,@zipCode); SELECT LAST_INSERT_ID();";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        FillCarrier(command, carrier);
        try
        {
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        }
        catch (MySqlException ex)
        {
            throw TranslateWriteException(ex);
        }
    }

    public async Task UpdateCarrierAsync(Carrier carrier, CancellationToken cancellationToken = default)
    {
        const string sql = @"UPDATE sales_carriers SET code=@code,name=@name,document=@document,contact_name=@contactName,email=@email,phone=@phone,street=@street,number=@number,district=@district,city=@city,state=@state,zip_code=@zipCode WHERE id=@id";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        FillCarrier(command, carrier);
        command.Parameters.AddWithValue("@id", carrier.Id);
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException ex)
        {
            throw TranslateWriteException(ex);
        }
    }

    public async Task<int> SaveCarrierAsync(Carrier carrier, IReadOnlyList<int> municipalityIds, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var carrierId = carrier.Id;
            if (carrierId > 0)
            {
                const string updateSql = @"UPDATE sales_carriers SET code=@code,name=@name,document=@document,contact_name=@contactName,email=@email,phone=@phone,street=@street,number=@number,district=@district,city=@city,state=@state,zip_code=@zipCode WHERE id=@id";
                await using var update = new MySqlCommand(updateSql, connection, (MySqlTransaction)tx);
                FillCarrier(update, carrier);
                update.Parameters.AddWithValue("@id", carrier.Id);
                await update.ExecuteNonQueryAsync(cancellationToken);
            }
            else
            {
                const string insertSql = @"INSERT INTO sales_carriers (code,name,document,contact_name,email,phone,street,number,district,city,state,zip_code) VALUES (@code,@name,@document,@contactName,@email,@phone,@street,@number,@district,@city,@state,@zipCode); SELECT LAST_INSERT_ID();";
                await using var insert = new MySqlCommand(insertSql, connection, (MySqlTransaction)tx);
                FillCarrier(insert, carrier);
                carrierId = Convert.ToInt32(await insert.ExecuteScalarAsync(cancellationToken));
            }

            await ReplaceCarrierServiceAreasAsync(connection, (MySqlTransaction)tx, carrierId, municipalityIds, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return carrierId;
        }
        catch (MySqlException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            throw TranslateWriteException(ex);
        }
    }

    public async Task<int> CreateQuoteAsync(QuoteDraft draft, SalesTotals totals, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        var quoteNumber = await NextNumberAsync(connection, (MySqlTransaction)tx, "sales_quotes", "quote_number", cancellationToken);
        var quoteId = await InsertQuoteHeaderAsync(connection, (MySqlTransaction)tx, quoteNumber, draft, totals, SalesStatuses.QuoteOpen, null, cancellationToken);
        await InsertQuoteItemsAsync(connection, (MySqlTransaction)tx, quoteId, draft.Items, cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return quoteId;
    }

    public async Task UpdateQuoteAsync(int quoteId, QuoteDraft draft, SalesTotals totals, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        const string headerSql = @"UPDATE sales_quotes SET customer_id=@customerId, carrier_id=@carrierId, subtotal=@subtotal, tax_amount=0, discount_amount=@discount, freight_amount=@freight, total_amount=@total WHERE id=@id AND status='ABERTO'";
        await using (var headerCmd = new MySqlCommand(headerSql, connection, (MySqlTransaction)tx))
        {
            headerCmd.Parameters.AddWithValue("@id", quoteId);
            headerCmd.Parameters.AddWithValue("@customerId", draft.CustomerId);
            headerCmd.Parameters.AddWithValue("@carrierId", draft.CarrierId.HasValue ? draft.CarrierId.Value : DBNull.Value);
            headerCmd.Parameters.AddWithValue("@subtotal", totals.Subtotal);
            headerCmd.Parameters.AddWithValue("@discount", draft.DiscountAmount);
            headerCmd.Parameters.AddWithValue("@freight", draft.FreightAmount);
            headerCmd.Parameters.AddWithValue("@total", totals.GrandTotal);
            await headerCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteItems = new MySqlCommand("DELETE FROM sales_quote_items WHERE quote_id=@id", connection, (MySqlTransaction)tx))
        {
            deleteItems.Parameters.AddWithValue("@id", quoteId);
            await deleteItems.ExecuteNonQueryAsync(cancellationToken);
        }

        await InsertQuoteItemsAsync(connection, (MySqlTransaction)tx, quoteId, draft.Items, cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task<QuoteData?> GetQuoteAsync(int quoteId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        const string headerSql = "SELECT id, quote_number, customer_id, carrier_id, discount_amount, freight_amount, status FROM sales_quotes WHERE id=@id";
        int? customerId = null;
        int? carrierId = null;
        int number = 0;
        decimal discount = 0;
        decimal freight = 0;
        string status = SalesStatuses.QuoteOpen;

        await using (var headerCmd = new MySqlCommand(headerSql, connection))
        {
            headerCmd.Parameters.AddWithValue("@id", quoteId);
            await using var reader = await headerCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            number = reader.GetInt32("quote_number");
            customerId = reader.GetInt32("customer_id");
            carrierId = reader.IsDBNull(reader.GetOrdinal("carrier_id")) ? null : reader.GetInt32("carrier_id");
            discount = reader.GetDecimal("discount_amount");
            freight = reader.GetDecimal("freight_amount");
            status = reader.GetString("status");
        }

        const string itemsSql = "SELECT product_id, description, quantity, unit_price FROM sales_quote_items WHERE quote_id=@id";
        var items = new List<SalesItemDraft>();
        await using (var itemsCmd = new MySqlCommand(itemsSql, connection))
        {
            itemsCmd.Parameters.AddWithValue("@id", quoteId);
            await using var itemsReader = await itemsCmd.ExecuteReaderAsync(cancellationToken);
            while (await itemsReader.ReadAsync(cancellationToken))
            {
                items.Add(new SalesItemDraft
                {
                    ProductId = itemsReader.GetInt32("product_id"),
                    Description = itemsReader.GetString("description"),
                    Quantity = itemsReader.GetDecimal("quantity"),
                    UnitPrice = itemsReader.GetDecimal("unit_price")
                });
            }
        }

        return new QuoteData
        {
            Id = quoteId,
            Number = number,
            CustomerId = customerId.Value,
            CarrierId = carrierId,
            DiscountAmount = discount,
            FreightAmount = freight,
            Status = status,
            Items = items
        };
    }

    public async Task<IReadOnlyList<QuoteSummary>> GetQuotesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT q.id, q.quote_number, c.name customer_name, q.total_amount, q.status, q.created_at
FROM sales_quotes q
JOIN sales_customers c ON c.id = q.customer_id
ORDER BY q.id DESC";

        var items = new List<QuoteSummary>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new QuoteSummary
            {
                Id = reader.GetInt32("id"),
                Number = reader.GetInt32("quote_number"),
                CustomerName = reader.GetString("customer_name"),
                TotalAmount = reader.GetDecimal("total_amount"),
                Status = reader.GetString("status"),
                CreatedAt = reader.GetDateTime("created_at")
            });
        }
        return items;
    }

    public async Task<int> ApproveQuoteAsync(int quoteId, string paymentTerm, int? carrierId, decimal freightAmount, string freightType, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        const string getQuoteSql = @"SELECT customer_id, subtotal, discount_amount, status FROM sales_quotes WHERE id=@id FOR UPDATE";
        int customerId;
        decimal subtotal;
        decimal discount;
        string status;

        await using (var cmd = new MySqlCommand(getQuoteSql, connection, (MySqlTransaction)tx))
        {
            cmd.Parameters.AddWithValue("@id", quoteId);
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await r.ReadAsync(cancellationToken)) throw new InvalidOperationException("Orcamento nao encontrado.");
            status = r.GetString("status");
            customerId = r.GetInt32("customer_id");
            subtotal = r.GetDecimal("subtotal");
            discount = r.GetDecimal("discount_amount");
        }
        var total = subtotal - discount + freightAmount;

        if (!string.Equals(status, SalesStatuses.QuoteOpen, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Somente orcamentos abertos podem ser aprovados.");
        }

        var orderNumber = await NextNumberAsync(connection, (MySqlTransaction)tx, "sales_orders", "order_number", cancellationToken);

        const string insertOrderSql = @"
INSERT INTO sales_orders (order_number, customer_id, carrier_id, subtotal, tax_amount, discount_amount, freight_amount, freight_type, total_amount, payment_term, status, source_type, source_quote_id)
VALUES (@number, @customerId, @carrierId, @subtotal, 0, @discount, @freight, @freightType, @total, @paymentTerm, @status, 'ORCAMENTO', @quoteId);
SELECT LAST_INSERT_ID();";

        int orderId;
        await using (var ins = new MySqlCommand(insertOrderSql, connection, (MySqlTransaction)tx))
        {
            ins.Parameters.AddWithValue("@number", orderNumber);
            ins.Parameters.AddWithValue("@customerId", customerId);
            ins.Parameters.AddWithValue("@carrierId", carrierId.HasValue ? carrierId.Value : DBNull.Value);
            ins.Parameters.AddWithValue("@subtotal", subtotal);
            ins.Parameters.AddWithValue("@discount", discount);
            ins.Parameters.AddWithValue("@freight", freightAmount);
            ins.Parameters.AddWithValue("@freightType", string.IsNullOrWhiteSpace(freightType) ? "CIF" : freightType.Trim());
            ins.Parameters.AddWithValue("@total", total);
            ins.Parameters.AddWithValue("@paymentTerm", string.IsNullOrWhiteSpace(paymentTerm) ? "A_VISTA" : paymentTerm.Trim());
            ins.Parameters.AddWithValue("@status", SalesStatuses.OrderOpen);
            ins.Parameters.AddWithValue("@quoteId", quoteId);
            orderId = Convert.ToInt32(await ins.ExecuteScalarAsync(cancellationToken));
        }

        const string copyItemsSql = @"
INSERT INTO sales_order_items (order_id, product_id, description, quantity, unit_price, tax_rate, line_total)
SELECT @orderId, product_id, description, quantity, unit_price, 0, line_total
FROM sales_quote_items WHERE quote_id = @quoteId;";

        await using (var copy = new MySqlCommand(copyItemsSql, connection, (MySqlTransaction)tx))
        {
            copy.Parameters.AddWithValue("@orderId", orderId);
            copy.Parameters.AddWithValue("@quoteId", quoteId);
            await copy.ExecuteNonQueryAsync(cancellationToken);
        }

        await ApplyStockOutputByOrderIdAsync(connection, (MySqlTransaction)tx, orderId, "VENDA_PEDIDO", quoteId, cancellationToken);

        const string updateQuoteSql = "UPDATE sales_quotes SET status=@status, approved_order_id=@orderId WHERE id=@quoteId";
        await using (var upd = new MySqlCommand(updateQuoteSql, connection, (MySqlTransaction)tx))
        {
            upd.Parameters.AddWithValue("@status", SalesStatuses.QuoteApproved);
            upd.Parameters.AddWithValue("@orderId", orderId);
            upd.Parameters.AddWithValue("@quoteId", quoteId);
            await upd.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
        return orderId;
    }

    public async Task RejectQuoteAsync(int quoteId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE sales_quotes SET status=@status WHERE id=@id AND status=@open";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@status", SalesStatuses.QuoteRejected);
        cmd.Parameters.AddWithValue("@open", SalesStatuses.QuoteOpen);
        cmd.Parameters.AddWithValue("@id", quoteId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> CreateOrderAsync(OrderDraft draft, SalesTotals totals, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        var orderNumber = await NextNumberAsync(connection, (MySqlTransaction)tx, "sales_orders", "order_number", cancellationToken);
        const string orderSql = @"
INSERT INTO sales_orders (order_number, customer_id, carrier_id, subtotal, tax_amount, discount_amount, freight_amount, freight_type, total_amount, payment_term, status, source_type)
VALUES (@number, @customerId, @carrierId, @subtotal, 0, @discount, @freight, @freightType, @total, @paymentTerm, @status, 'MANUAL');
SELECT LAST_INSERT_ID();";

        int orderId;
        await using (var cmd = new MySqlCommand(orderSql, connection, (MySqlTransaction)tx))
        {
            cmd.Parameters.AddWithValue("@number", orderNumber);
            cmd.Parameters.AddWithValue("@customerId", draft.CustomerId);
            cmd.Parameters.AddWithValue("@carrierId", draft.CarrierId.HasValue ? draft.CarrierId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@subtotal", totals.Subtotal);
            cmd.Parameters.AddWithValue("@discount", draft.DiscountAmount);
            cmd.Parameters.AddWithValue("@freight", draft.FreightAmount);
            cmd.Parameters.AddWithValue("@freightType", string.IsNullOrWhiteSpace(draft.FreightType) ? "CIF" : draft.FreightType.Trim());
            cmd.Parameters.AddWithValue("@total", totals.GrandTotal);
            cmd.Parameters.AddWithValue("@paymentTerm", string.IsNullOrWhiteSpace(draft.PaymentTerm) ? "A_VISTA" : draft.PaymentTerm.Trim());
            cmd.Parameters.AddWithValue("@status", SalesStatuses.OrderOpen);
            orderId = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
        }

        await InsertOrderItemsAsync(connection, (MySqlTransaction)tx, orderId, draft.Items, cancellationToken);
        await ApplyStockOutputFromDraftAsync(connection, (MySqlTransaction)tx, orderId, draft.Items, "VENDA_PEDIDO", null, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return orderId;
    }

    public async Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT o.id, o.order_number, c.name customer_name, o.total_amount, o.payment_term, o.status, o.freight_type, o.source_type, o.source_quote_id, q.quote_number source_quote_number, o.created_at
FROM sales_orders o
JOIN sales_customers c ON c.id = o.customer_id
LEFT JOIN sales_quotes q ON q.id = o.source_quote_id
ORDER BY o.id DESC";

        var list = new List<OrderSummary>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await r.ReadAsync(cancellationToken))
        {
            list.Add(new OrderSummary
            {
                Id = r.GetInt32("id"),
                Number = r.GetInt32("order_number"),
                CustomerName = r.GetString("customer_name"),
                TotalAmount = r.GetDecimal("total_amount"),
                PaymentTerm = r.GetString("payment_term"),
                Status = r.GetString("status"),
                FreightType = r.IsDBNull(r.GetOrdinal("freight_type")) ? "CIF" : r.GetString("freight_type"),
                SourceType = r.GetString("source_type"),
                SourceQuoteId = r.IsDBNull(r.GetOrdinal("source_quote_id")) ? null : r.GetInt32("source_quote_id"),
                SourceQuoteNumber = r.IsDBNull(r.GetOrdinal("source_quote_number")) ? null : r.GetInt32("source_quote_number"),
                CreatedAt = r.GetDateTime("created_at")
            });
        }

        return list;
    }

    public async Task MarkOrderReadyForInvoiceAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE sales_orders SET status=@status WHERE id=@id AND status=@open";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@status", SalesStatuses.OrderReadyForInvoice);
        cmd.Parameters.AddWithValue("@open", SalesStatuses.OrderOpen);
        cmd.Parameters.AddWithValue("@id", orderId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> GenerateInvoiceAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        const string validateSql = "SELECT status, customer_id, total_amount, payment_term FROM sales_orders WHERE id=@id FOR UPDATE";
        string orderStatus;
        int customerId;
        decimal totalAmount;
        string paymentTerm;
        await using (var validate = new MySqlCommand(validateSql, connection, (MySqlTransaction)tx))
        {
            validate.Parameters.AddWithValue("@id", orderId);
            await using var reader = await validate.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Pedido nao encontrado.");
            }
            orderStatus = reader.GetString("status");
            customerId = reader.GetInt32("customer_id");
            totalAmount = reader.GetDecimal("total_amount");
            paymentTerm = reader.GetString("payment_term");
        }

        if (!string.Equals(orderStatus, SalesStatuses.OrderReadyForInvoice, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Pedido nao esta liberado para nota fiscal ou ja foi faturado.");
        }

        const string existingInvoiceSql = "SELECT COUNT(*) FROM sales_invoices WHERE order_id=@orderId";
        await using (var existing = new MySqlCommand(existingInvoiceSql, connection, (MySqlTransaction)tx))
        {
            existing.Parameters.AddWithValue("@orderId", orderId);
            var exists = Convert.ToInt32(await existing.ExecuteScalarAsync(cancellationToken)) > 0;
            if (exists)
            {
                throw new InvalidOperationException("Este pedido ja possui nota fiscal gerada.");
            }
        }

        var invoiceNumber = await NextNumberAsync(connection, (MySqlTransaction)tx, "sales_invoices", "invoice_number", cancellationToken);

        const string insertInvoiceSql = @"
INSERT INTO sales_invoices (invoice_number, order_id, customer_id, total_amount, payment_term, source_type, status)
VALUES (@number, @orderId, @customerId, @totalAmount, @paymentTerm, 'PEDIDO', 'GERADA');
SELECT LAST_INSERT_ID();";

        int invoiceId;
        await using (var cmd = new MySqlCommand(insertInvoiceSql, connection, (MySqlTransaction)tx))
        {
            cmd.Parameters.AddWithValue("@number", invoiceNumber);
            cmd.Parameters.AddWithValue("@orderId", orderId);
            cmd.Parameters.AddWithValue("@customerId", customerId);
            cmd.Parameters.AddWithValue("@totalAmount", totalAmount);
            cmd.Parameters.AddWithValue("@paymentTerm", paymentTerm);
            invoiceId = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
        }

        await using (var upd = new MySqlCommand("UPDATE sales_orders SET status=@status WHERE id=@id", connection, (MySqlTransaction)tx))
        {
            upd.Parameters.AddWithValue("@status", SalesStatuses.OrderInvoiced);
            upd.Parameters.AddWithValue("@id", orderId);
            await upd.ExecuteNonQueryAsync(cancellationToken);
        }

        await CreateReceivablesFromInvoiceAsync(connection, (MySqlTransaction)tx, orderId, invoiceId, customerId, totalAmount, paymentTerm, cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return invoiceId;
    }

    public async Task<int> CreateDirectInvoiceAsync(OrderDraft draft, SalesTotals totals, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        var orderNumber = await NextNumberAsync(connection, (MySqlTransaction)tx, "sales_orders", "order_number", cancellationToken);
        const string orderSql = @"
INSERT INTO sales_orders (order_number, customer_id, carrier_id, subtotal, tax_amount, discount_amount, freight_amount, freight_type, total_amount, payment_term, status, source_type)
VALUES (@number, @customerId, @carrierId, @subtotal, 0, @discount, @freight, @freightType, @total, @paymentTerm, @status, 'NF_DIRETA');
SELECT LAST_INSERT_ID();";

        int orderId;
        await using (var order = new MySqlCommand(orderSql, connection, (MySqlTransaction)tx))
        {
            order.Parameters.AddWithValue("@number", orderNumber);
            order.Parameters.AddWithValue("@customerId", draft.CustomerId);
            order.Parameters.AddWithValue("@carrierId", draft.CarrierId.HasValue ? draft.CarrierId.Value : DBNull.Value);
            order.Parameters.AddWithValue("@subtotal", totals.Subtotal);
            order.Parameters.AddWithValue("@discount", draft.DiscountAmount);
            order.Parameters.AddWithValue("@freight", draft.FreightAmount);
            order.Parameters.AddWithValue("@freightType", string.IsNullOrWhiteSpace(draft.FreightType) ? "CIF" : draft.FreightType.Trim());
            order.Parameters.AddWithValue("@total", totals.GrandTotal);
            order.Parameters.AddWithValue("@paymentTerm", string.IsNullOrWhiteSpace(draft.PaymentTerm) ? "A_VISTA" : draft.PaymentTerm.Trim());
            order.Parameters.AddWithValue("@status", SalesStatuses.OrderInvoiced);
            orderId = Convert.ToInt32(await order.ExecuteScalarAsync(cancellationToken));
        }

        await InsertOrderItemsAsync(connection, (MySqlTransaction)tx, orderId, draft.Items, cancellationToken);
        await ApplyStockOutputFromDraftAsync(connection, (MySqlTransaction)tx, orderId, draft.Items, "VENDA_NF_DIRETA", null, cancellationToken);

        var invoiceNumber = await NextNumberAsync(connection, (MySqlTransaction)tx, "sales_invoices", "invoice_number", cancellationToken);
        const string invoiceSql = @"
INSERT INTO sales_invoices (invoice_number, order_id, customer_id, total_amount, payment_term, source_type, status)
VALUES (@number, @orderId, @customerId, @totalAmount, @paymentTerm, 'DIRETA', 'GERADA');
SELECT LAST_INSERT_ID();";

        int invoiceId;
        await using (var invoice = new MySqlCommand(invoiceSql, connection, (MySqlTransaction)tx))
        {
            invoice.Parameters.AddWithValue("@number", invoiceNumber);
            invoice.Parameters.AddWithValue("@orderId", orderId);
            invoice.Parameters.AddWithValue("@customerId", draft.CustomerId);
            invoice.Parameters.AddWithValue("@totalAmount", totals.GrandTotal);
            invoice.Parameters.AddWithValue("@paymentTerm", string.IsNullOrWhiteSpace(draft.PaymentTerm) ? "A_VISTA" : draft.PaymentTerm.Trim());
            invoiceId = Convert.ToInt32(await invoice.ExecuteScalarAsync(cancellationToken));
        }

        await CreateReceivablesFromInvoiceAsync(connection, (MySqlTransaction)tx, orderId, invoiceId, draft.CustomerId, totals.GrandTotal, draft.PaymentTerm, cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return invoiceId;
    }

    public async Task<IReadOnlyList<InvoiceSummary>> GetInvoicesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT i.id,
       i.invoice_number,
       o.order_number,
       COALESCE(c_invoice.name, c_order.name, 'Cliente nao informado') customer_name,
       COALESCE(i.total_amount, o.total_amount, 0) total_amount,
       i.source_type,
       i.created_at
FROM sales_invoices i
LEFT JOIN sales_orders o ON o.id = i.order_id
LEFT JOIN sales_customers c_order ON c_order.id = o.customer_id
LEFT JOIN sales_customers c_invoice ON c_invoice.id = i.customer_id
ORDER BY i.id DESC";

        var list = new List<InvoiceSummary>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, connection);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await r.ReadAsync(cancellationToken))
        {
            list.Add(new InvoiceSummary
            {
                Id = r.GetInt32("id"),
                Number = r.GetInt32("invoice_number"),
                OrderNumber = r.IsDBNull(r.GetOrdinal("order_number")) ? null : r.GetInt32("order_number"),
                CustomerName = r.GetString("customer_name"),
                TotalAmount = r.GetDecimal("total_amount"),
                SourceType = r.GetString("source_type"),
                CreatedAt = r.GetDateTime("created_at")
            });
        }
        return list;
    }

    private static async Task<int> NextNumberAsync(MySqlConnection connection, MySqlTransaction tx, string table, string column, CancellationToken ct)
    {
        var sql = $"SELECT IFNULL(MAX({column}), 0) + 1 FROM {table}";
        await using var cmd = new MySqlCommand(sql, connection, tx);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    private static async Task<int> InsertQuoteHeaderAsync(MySqlConnection connection, MySqlTransaction tx, int number, QuoteDraft draft, SalesTotals totals, string status, int? approvedOrderId, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO sales_quotes (quote_number, customer_id, carrier_id, subtotal, tax_amount, discount_amount, freight_amount, total_amount, status, approved_order_id)
VALUES (@number, @customerId, @carrierId, @subtotal, 0, @discount, @freight, @total, @status, @approvedOrderId);
SELECT LAST_INSERT_ID();";

        await using var cmd = new MySqlCommand(sql, connection, tx);
        cmd.Parameters.AddWithValue("@number", number);
        cmd.Parameters.AddWithValue("@customerId", draft.CustomerId);
        cmd.Parameters.AddWithValue("@carrierId", draft.CarrierId.HasValue ? draft.CarrierId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@subtotal", totals.Subtotal);
        cmd.Parameters.AddWithValue("@discount", draft.DiscountAmount);
        cmd.Parameters.AddWithValue("@freight", draft.FreightAmount);
        cmd.Parameters.AddWithValue("@total", totals.GrandTotal);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@approvedOrderId", approvedOrderId.HasValue ? approvedOrderId.Value : DBNull.Value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    private static async Task InsertQuoteItemsAsync(MySqlConnection connection, MySqlTransaction tx, int quoteId, IReadOnlyList<SalesItemDraft> items, CancellationToken ct)
    {
        const string sql = @"INSERT INTO sales_quote_items (quote_id, product_id, description, quantity, unit_price, tax_rate, line_total) VALUES (@quoteId, @productId, @description, @quantity, @unitPrice, 0, @lineTotal)";
        foreach (var item in items)
        {
            await using var cmd = new MySqlCommand(sql, connection, tx);
            cmd.Parameters.AddWithValue("@quoteId", quoteId);
            cmd.Parameters.AddWithValue("@productId", item.ProductId);
            cmd.Parameters.AddWithValue("@description", item.Description);
            cmd.Parameters.AddWithValue("@quantity", item.Quantity);
            cmd.Parameters.AddWithValue("@unitPrice", item.UnitPrice);
            cmd.Parameters.AddWithValue("@lineTotal", decimal.Round(item.Quantity * item.UnitPrice, 2));
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    private static async Task InsertOrderItemsAsync(MySqlConnection connection, MySqlTransaction tx, int orderId, IReadOnlyList<SalesItemDraft> items, CancellationToken ct)
    {
        const string sql = @"INSERT INTO sales_order_items (order_id, product_id, description, quantity, unit_price, tax_rate, line_total) VALUES (@orderId, @productId, @description, @quantity, @unitPrice, 0, @lineTotal)";
        foreach (var item in items)
        {
            await using var cmd = new MySqlCommand(sql, connection, tx);
            cmd.Parameters.AddWithValue("@orderId", orderId);
            cmd.Parameters.AddWithValue("@productId", item.ProductId);
            cmd.Parameters.AddWithValue("@description", item.Description);
            cmd.Parameters.AddWithValue("@quantity", item.Quantity);
            cmd.Parameters.AddWithValue("@unitPrice", item.UnitPrice);
            cmd.Parameters.AddWithValue("@lineTotal", decimal.Round(item.Quantity * item.UnitPrice, 2));
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    private static async Task ApplyStockOutputFromDraftAsync(MySqlConnection connection, MySqlTransaction tx, int orderId, IReadOnlyList<SalesItemDraft> items, string sourceType, int? sourceId, CancellationToken ct)
    {
        foreach (var item in items)
        {
            await DecreaseStockAsync(connection, tx, item.ProductId, item.Quantity, sourceType, orderId, sourceId, ct);
        }
    }

    private static async Task ApplyStockOutputByOrderIdAsync(MySqlConnection connection, MySqlTransaction tx, int orderId, string sourceType, int? sourceId, CancellationToken ct)
    {
        const string sql = "SELECT product_id, quantity FROM sales_order_items WHERE order_id=@orderId";
        var items = new List<(int ProductId, decimal Quantity)>();
        await using (var cmd = new MySqlCommand(sql, connection, tx))
        {
            cmd.Parameters.AddWithValue("@orderId", orderId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                items.Add((r.GetInt32("product_id"), r.GetDecimal("quantity")));
            }
        }

        foreach (var item in items)
        {
            await DecreaseStockAsync(connection, tx, item.ProductId, item.Quantity, sourceType, orderId, sourceId, ct);
        }
    }

    private static async Task DecreaseStockAsync(MySqlConnection connection, MySqlTransaction tx, int productId, decimal quantity, string sourceType, int sourceOrderId, int? sourceQuoteId, CancellationToken ct)
    {
        const string updateSql = "UPDATE sales_products SET stock_quantity = stock_quantity - @qty WHERE id=@productId AND stock_quantity >= @qty";
        await using (var upd = new MySqlCommand(updateSql, connection, tx))
        {
            upd.Parameters.AddWithValue("@qty", quantity);
            upd.Parameters.AddWithValue("@productId", productId);
            var affectedRows = await upd.ExecuteNonQueryAsync(ct);
            if (affectedRows <= 0)
            {
                await using var balanceCmd = new MySqlCommand("SELECT sku, stock_quantity FROM sales_products WHERE id=@productId", connection, tx);
                balanceCmd.Parameters.AddWithValue("@productId", productId);
                await using var reader = await balanceCmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    var sku = reader.GetString("sku");
                    var currentBalance = reader.GetDecimal("stock_quantity");
                    throw new InvalidOperationException($"Estoque insuficiente para o produto {sku}. Saldo atual: {currentBalance:N2}, solicitado: {quantity:N2}.");
                }

                throw new InvalidOperationException("Produto nao encontrado para baixa de estoque.");
            }
        }

        const string moveSql = @"
INSERT INTO stock_movements (product_id, movement_type, quantity, source_type, source_id, notes)
VALUES (@productId, 'SAIDA', @qty, @sourceType, @sourceId, @notes);";
        await using (var move = new MySqlCommand(moveSql, connection, tx))
        {
            move.Parameters.AddWithValue("@productId", productId);
            move.Parameters.AddWithValue("@qty", quantity);
            move.Parameters.AddWithValue("@sourceType", sourceType);
            move.Parameters.AddWithValue("@sourceId", sourceOrderId);
            move.Parameters.AddWithValue("@notes", sourceQuoteId.HasValue ? $"Origem Orcamento #{sourceQuoteId.Value}" : "Saida por venda");
            await move.ExecuteNonQueryAsync(ct);
        }
    }

    private static async Task CreateReceivablesFromInvoiceAsync(MySqlConnection connection, MySqlTransaction tx, int orderId, int invoiceId, int customerId, decimal totalAmount, string paymentTerm, CancellationToken ct)
    {
        var installments = PaymentInstallmentCalculator.BuildInstallments(totalAmount, DateTime.Today, paymentTerm);
        const string sql = @"
INSERT INTO finance_receivables (order_id, invoice_id, customer_id, installment_number, due_date, amount, payment_term, status)
VALUES (@orderId, @invoiceId, @customerId, @installment, @dueDate, @amount, @paymentTerm, 'PENDENTE');";

        foreach (var installment in installments)
        {
            await using var cmd = new MySqlCommand(sql, connection, tx);
            cmd.Parameters.AddWithValue("@orderId", orderId);
            cmd.Parameters.AddWithValue("@invoiceId", invoiceId);
            cmd.Parameters.AddWithValue("@customerId", customerId);
            cmd.Parameters.AddWithValue("@installment", installment.Number);
            cmd.Parameters.AddWithValue("@dueDate", installment.DueDate);
            cmd.Parameters.AddWithValue("@amount", installment.Amount);
            cmd.Parameters.AddWithValue("@paymentTerm", paymentTerm);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    // === Product Classifications ===

    public async Task<IReadOnlyList<ProductClassification>> GetClassificationsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id, code, name FROM product_classifications ORDER BY code";
        var items = new List<ProductClassification>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(new ProductClassification { Id = reader.GetInt32("id"), Code = reader.GetString("code"), Name = reader.GetString("name") });
        return items;
    }

    public async Task<int> AddClassificationAsync(ProductClassification classification, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO product_classifications (code, name) VALUES (@code, @name); SELECT LAST_INSERT_ID();";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@code", classification.Code);
        command.Parameters.AddWithValue("@name", classification.Name);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task UpdateClassificationAsync(ProductClassification classification, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE product_classifications SET code=@code, name=@name WHERE id=@id";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@code", classification.Code);
        command.Parameters.AddWithValue("@name", classification.Name);
        command.Parameters.AddWithValue("@id", classification.Id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteClassificationAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        const string hasProductsSql = @"
SELECT COUNT(*)
FROM sales_products p
JOIN product_sub_classifications s ON s.id = p.sub_classification_id
WHERE s.classification_id=@id;";

        await using (var check = new MySqlCommand(hasProductsSql, connection, (MySqlTransaction)tx))
        {
            check.Parameters.AddWithValue("@id", id);
            var linkedProducts = Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken));
            if (linkedProducts > 0)
            {
                throw new InvalidOperationException("Nao e possivel excluir a classificacao porque existem produtos vinculados a suas sub-classificacoes.");
            }
        }

        const string deleteValuesSql = @"
DELETE cv
FROM product_custom_values cv
JOIN product_custom_fields f ON f.id = cv.field_id
JOIN product_sub_classifications s ON s.id = f.sub_classification_id
WHERE s.classification_id=@id;";
        await using (var deleteValues = new MySqlCommand(deleteValuesSql, connection, (MySqlTransaction)tx))
        {
            deleteValues.Parameters.AddWithValue("@id", id);
            await deleteValues.ExecuteNonQueryAsync(cancellationToken);
        }

        const string deleteFieldsSql = @"
DELETE f
FROM product_custom_fields f
JOIN product_sub_classifications s ON s.id = f.sub_classification_id
WHERE s.classification_id=@id;";
        await using (var deleteFields = new MySqlCommand(deleteFieldsSql, connection, (MySqlTransaction)tx))
        {
            deleteFields.Parameters.AddWithValue("@id", id);
            await deleteFields.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteSubs = new MySqlCommand("DELETE FROM product_sub_classifications WHERE classification_id=@id", connection, (MySqlTransaction)tx))
        {
            deleteSubs.Parameters.AddWithValue("@id", id);
            await deleteSubs.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteClass = new MySqlCommand("DELETE FROM product_classifications WHERE id=@id", connection, (MySqlTransaction)tx))
        {
            deleteClass.Parameters.AddWithValue("@id", id);
            await deleteClass.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductSubClassification>> GetSubClassificationsAsync(int classificationId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id, classification_id, code, name FROM product_sub_classifications WHERE classification_id=@classId ORDER BY code";
        var items = new List<ProductSubClassification>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@classId", classificationId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(new ProductSubClassification { Id = reader.GetInt32("id"), ClassificationId = reader.GetInt32("classification_id"), Code = reader.GetString("code"), Name = reader.GetString("name") });
        return items;
    }

    public async Task<IReadOnlyList<ProductSubClassification>> GetAllSubClassificationsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id, classification_id, code, name FROM product_sub_classifications ORDER BY code";
        var items = new List<ProductSubClassification>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(new ProductSubClassification { Id = reader.GetInt32("id"), ClassificationId = reader.GetInt32("classification_id"), Code = reader.GetString("code"), Name = reader.GetString("name") });
        return items;
    }

    public async Task<int> AddSubClassificationAsync(ProductSubClassification sub, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO product_sub_classifications (classification_id, code, name) VALUES (@classId, @code, @name); SELECT LAST_INSERT_ID();";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@classId", sub.ClassificationId);
        command.Parameters.AddWithValue("@code", sub.Code);
        command.Parameters.AddWithValue("@name", sub.Name);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task UpdateSubClassificationAsync(ProductSubClassification sub, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE product_sub_classifications SET code=@code, name=@name WHERE id=@id";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@code", sub.Code);
        command.Parameters.AddWithValue("@name", sub.Name);
        command.Parameters.AddWithValue("@id", sub.Id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteSubClassificationAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        await using (var check = new MySqlCommand("SELECT COUNT(*) FROM sales_products WHERE sub_classification_id=@id", connection, (MySqlTransaction)tx))
        {
            check.Parameters.AddWithValue("@id", id);
            var linkedProducts = Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken));
            if (linkedProducts > 0)
            {
                throw new InvalidOperationException("Nao e possivel excluir a sub-classificacao porque existem produtos vinculados a ela.");
            }
        }

        const string deleteValuesSql = @"
DELETE cv
FROM product_custom_values cv
JOIN product_custom_fields f ON f.id = cv.field_id
WHERE f.sub_classification_id=@id;";
        await using (var deleteValues = new MySqlCommand(deleteValuesSql, connection, (MySqlTransaction)tx))
        {
            deleteValues.Parameters.AddWithValue("@id", id);
            await deleteValues.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteFields = new MySqlCommand("DELETE FROM product_custom_fields WHERE sub_classification_id=@id", connection, (MySqlTransaction)tx))
        {
            deleteFields.Parameters.AddWithValue("@id", id);
            await deleteFields.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteSub = new MySqlCommand("DELETE FROM product_sub_classifications WHERE id=@id", connection, (MySqlTransaction)tx))
        {
            deleteSub.Parameters.AddWithValue("@id", id);
            await deleteSub.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductCustomField>> GetCustomFieldsAsync(int subClassificationId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id, sub_classification_id, field_name, is_required FROM product_custom_fields WHERE sub_classification_id=@subId ORDER BY field_name";
        var items = new List<ProductCustomField>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@subId", subClassificationId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(new ProductCustomField { Id = reader.GetInt32("id"), SubClassificationId = reader.GetInt32("sub_classification_id"), FieldName = reader.GetString("field_name"), IsRequired = reader.GetBoolean("is_required") });
        return items;
    }

    public async Task<int> AddCustomFieldAsync(ProductCustomField field, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO product_custom_fields (sub_classification_id, field_name, is_required) VALUES (@subId, @name, @required); SELECT LAST_INSERT_ID();";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@subId", field.SubClassificationId);
        command.Parameters.AddWithValue("@name", field.FieldName);
        command.Parameters.AddWithValue("@required", field.IsRequired);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task UpdateCustomFieldAsync(ProductCustomField field, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE product_custom_fields SET field_name=@name, is_required=@required WHERE id=@id";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@name", field.FieldName);
        command.Parameters.AddWithValue("@required", field.IsRequired);
        command.Parameters.AddWithValue("@id", field.Id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteCustomFieldAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM product_custom_fields WHERE id=@id";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(int FieldId, string FieldName, string Value)>> GetProductCustomValuesAsync(int productId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT cv.field_id, cf.field_name, cv.value FROM product_custom_values cv JOIN product_custom_fields cf ON cf.id = cv.field_id WHERE cv.product_id=@productId";
        var items = new List<(int, string, string)>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@productId", productId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add((reader.GetInt32("field_id"), reader.GetString("field_name"), reader.GetString("value")));
        return items;
    }

    public async Task SaveProductCustomValuesAsync(int productId, IReadOnlyList<(int FieldId, string Value)> values, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        await ReplaceProductCustomValuesAsync(connection, (MySqlTransaction)tx, productId, values, cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    // === Product Brands ===

    public async Task<IReadOnlyList<ProductBrand>> GetBrandsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id, code, name FROM product_brands ORDER BY name";
        var items = new List<ProductBrand>();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ProductBrand { Id = reader.GetInt32("id"), Code = reader.GetString("code"), Name = reader.GetString("name") });
        }
        return items;
    }

    public async Task<int> AddBrandAsync(ProductBrand brand, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO product_brands (code, name) VALUES (@code, @name); SELECT LAST_INSERT_ID();";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@code", brand.Code.Trim());
        command.Parameters.AddWithValue("@name", brand.Name.Trim());
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task UpdateBrandAsync(ProductBrand brand, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE product_brands SET code=@code, name=@name WHERE id=@id";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@code", brand.Code.Trim());
        command.Parameters.AddWithValue("@name", brand.Name.Trim());
        command.Parameters.AddWithValue("@id", brand.Id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteBrandAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM product_brands WHERE id=@id";
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string? NullableString(MySqlDataReader reader, string column)
    {
        var ord = reader.GetOrdinal(column);
        return reader.IsDBNull(ord) ? null : reader.GetString(column);
    }

    private static void FillCustomer(MySqlCommand cmd, Customer c, string? codeOverride = null)
    {
        cmd.Parameters.AddWithValue("@code", (codeOverride ?? c.Code).Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("@name", c.Name);
        cmd.Parameters.AddWithValue("@document", c.Document);
        cmd.Parameters.AddWithValue("@isActive", c.IsActive);
        cmd.Parameters.AddWithValue("@contactName", c.ContactName);
        cmd.Parameters.AddWithValue("@email", c.Email);
        cmd.Parameters.AddWithValue("@phone", c.Phone);
        cmd.Parameters.AddWithValue("@street", c.Street);
        cmd.Parameters.AddWithValue("@number", c.Number);
        cmd.Parameters.AddWithValue("@district", c.District);
        cmd.Parameters.AddWithValue("@city", c.City);
        cmd.Parameters.AddWithValue("@state", c.State);
        cmd.Parameters.AddWithValue("@zipCode", c.ZipCode);
    }

    private static void FillProduct(MySqlCommand cmd, Product p, string? codeOverride = null)
    {
        cmd.Parameters.AddWithValue("@code", (object?)NormalizeProductCode(codeOverride ?? p.Code) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sku", p.Sku);
        cmd.Parameters.AddWithValue("@name", p.Name);
        cmd.Parameters.AddWithValue("@isActive", p.IsActive);
        cmd.Parameters.AddWithValue("@description", (object?)NormalizeOptionalText(p.Description) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@barcode", (object?)NormalizeOptionalText(p.Barcode) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@unitPrice", p.UnitPrice);
        cmd.Parameters.AddWithValue("@costPrice", p.CostPrice);
        cmd.Parameters.AddWithValue("@stockQty", p.Quantity);
        cmd.Parameters.AddWithValue("@imagePath", (object?)NormalizeOptionalText(p.ImagePath) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@color", (object?)NormalizeOptionalText(p.Color) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@subClassId", p.SubClassificationId.HasValue ? p.SubClassificationId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@ncm", (object?)NormalizeOptionalText(p.Ncm) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@weight", p.Weight);
        cmd.Parameters.AddWithValue("@width", p.Width);
        cmd.Parameters.AddWithValue("@height", p.Height);
        cmd.Parameters.AddWithValue("@depth", p.Depth);
        cmd.Parameters.AddWithValue("@voltage", (object?)NormalizeOptionalText(p.Voltage) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@brandId", p.BrandId.HasValue ? p.BrandId.Value : DBNull.Value);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string? NormalizeProductCode(string? value)
        => NormalizeOptionalText(value)?.ToUpperInvariant();

    private static async Task ReplaceProductCustomValuesAsync(MySqlConnection connection, MySqlTransaction tx, int productId, IReadOnlyList<(int FieldId, string Value)> values, CancellationToken cancellationToken)
    {
        await using (var del = new MySqlCommand("DELETE FROM product_custom_values WHERE product_id=@productId", connection, tx))
        {
            del.Parameters.AddWithValue("@productId", productId);
            await del.ExecuteNonQueryAsync(cancellationToken);
        }

        const string insertSql = "INSERT INTO product_custom_values (product_id, field_id, value) VALUES (@productId, @fieldId, @value)";
        foreach (var (fieldId, value) in values)
        {
            await using var cmd = new MySqlCommand(insertSql, connection, tx);
            cmd.Parameters.AddWithValue("@productId", productId);
            cmd.Parameters.AddWithValue("@fieldId", fieldId);
            cmd.Parameters.AddWithValue("@value", value);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task ReplaceCarrierServiceAreasAsync(MySqlConnection connection, MySqlTransaction tx, int carrierId, IReadOnlyList<int> municipalityIds, CancellationToken cancellationToken)
    {
        await using (var del = new MySqlCommand("DELETE FROM carrier_service_areas WHERE carrier_id = @carrierId", connection, tx))
        {
            del.Parameters.AddWithValue("@carrierId", carrierId);
            await del.ExecuteNonQueryAsync(cancellationToken);
        }

        if (municipalityIds.Count == 0)
            return;

        const string insertSql = "INSERT INTO carrier_service_areas (carrier_id, municipality_id) VALUES (@carrierId, @municipalityId)";
        await using var ins = new MySqlCommand(insertSql, connection, tx);
        ins.Parameters.AddWithValue("@carrierId", carrierId);
        ins.Parameters.AddWithValue("@municipalityId", 0);

        foreach (var munId in municipalityIds.Distinct())
        {
            ins.Parameters["@municipalityId"].Value = munId;
            await ins.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static bool ShouldRetryGeneratedCode(MySqlException ex, string indexName)
        => ex.Number == 1062 && ex.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase);

    private static Exception TranslateWriteException(MySqlException ex)
    {
        if (ex.Number != 1062)
            return ex;

        return ex.Message switch
        {
            var m when m.Contains("uq_sales_customers_code", StringComparison.OrdinalIgnoreCase)
                => new InvalidOperationException("Ja existe um cliente com este codigo."),
            var m when m.Contains("uq_sales_customers_document", StringComparison.OrdinalIgnoreCase)
                => new InvalidOperationException("Ja existe um cliente com este CPF/CNPJ."),
            var m when m.Contains("uq_sales_products_code", StringComparison.OrdinalIgnoreCase)
                => new InvalidOperationException("Ja existe um produto com este codigo."),
            var m when m.Contains("uq_sales_products_sku", StringComparison.OrdinalIgnoreCase)
                => new InvalidOperationException("Ja existe um produto com este SKU."),
            var m when m.Contains("uq_sales_products_barcode", StringComparison.OrdinalIgnoreCase)
                => new InvalidOperationException("Ja existe um produto com este codigo de barras."),
            var m when m.Contains("uq_sales_carriers_document", StringComparison.OrdinalIgnoreCase)
                => new InvalidOperationException("Ja existe uma transportadora com este CNPJ."),
            var m when m.Contains("uq_inventory_suppliers_tax_id", StringComparison.OrdinalIgnoreCase)
                => new InvalidOperationException("Ja existe um fornecedor com este CNPJ."),
            _ => new InvalidOperationException("Ja existe um cadastro com um identificador igual.")
        };
    }

    private static void FillCarrier(MySqlCommand cmd, Carrier c)
    {
        cmd.Parameters.AddWithValue("@code", c.Code);
        cmd.Parameters.AddWithValue("@name", c.Name);
        cmd.Parameters.AddWithValue("@document", c.Document);
        cmd.Parameters.AddWithValue("@contactName", (object?)c.ContactName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@email", (object?)c.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@phone", (object?)c.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@street", (object?)c.Street ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@number", (object?)c.Number ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@district", (object?)c.District ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@city", (object?)c.City ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@state", (object?)c.State ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@zipCode", (object?)c.ZipCode ?? DBNull.Value);
    }

    public async Task<IReadOnlyList<Municipality>> GetCarrierServiceAreasAsync(int carrierId, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT m.id, m.code, m.name, m.state FROM carrier_service_areas csa INNER JOIN municipalities m ON m.id = csa.municipality_id WHERE csa.carrier_id = @carrierId ORDER BY m.name, m.state";
        var items = new List<Municipality>();

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@carrierId", carrierId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new Municipality
            {
                Id = reader.GetInt32("id"),
                Code = reader.GetString("code"),
                Name = reader.GetString("name"),
                State = reader.GetString("state")
            });
        }

        return items;
    }

    public async Task SetCarrierServiceAreasAsync(int carrierId, IReadOnlyList<int> municipalityIds, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        await ReplaceCarrierServiceAreasAsync(connection, (MySqlTransaction)tx, carrierId, municipalityIds, cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
}

