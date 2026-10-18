using MySqlConnector;

namespace Erp.Infrastructure.MySql.Data;

public sealed class MySqlSchemaInitializer
{
    private const string SchemaSql = @"
CREATE TABLE IF NOT EXISTS municipalities (
  id INT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(30) NOT NULL DEFAULT '',
  name VARCHAR(120) NOT NULL,
  state VARCHAR(60) NOT NULL,
  country VARCHAR(100) NOT NULL DEFAULT 'Brasil',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS sales_customers (
  id INT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(30) NOT NULL DEFAULT '',
  name VARCHAR(160) NOT NULL,
  document VARCHAR(40) NOT NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  contact_name VARCHAR(120) NULL,
  email VARCHAR(160) NULL,
  phone VARCHAR(30) NULL,
  street VARCHAR(160) NULL,
  number VARCHAR(30) NULL,
  district VARCHAR(100) NULL,
  city VARCHAR(100) NULL,
  state VARCHAR(2) NULL,
  zip_code VARCHAR(20) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY uq_sales_customers_code (code),
  UNIQUE KEY uq_sales_customers_document (document)
);

CREATE TABLE IF NOT EXISTS product_classifications (
  id INT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(20) NOT NULL,
  name VARCHAR(120) NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS product_sub_classifications (
  id INT AUTO_INCREMENT PRIMARY KEY,
  classification_id INT NOT NULL,
  code VARCHAR(20) NOT NULL,
  name VARCHAR(120) NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_sub_class_class FOREIGN KEY (classification_id) REFERENCES product_classifications(id)
);

CREATE TABLE IF NOT EXISTS product_custom_fields (
  id INT AUTO_INCREMENT PRIMARY KEY,
  sub_classification_id INT NOT NULL,
  field_name VARCHAR(100) NOT NULL,
  is_required TINYINT(1) NOT NULL DEFAULT 0,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_custom_field_sub FOREIGN KEY (sub_classification_id) REFERENCES product_sub_classifications(id)
);

CREATE TABLE IF NOT EXISTS product_brands (
  id INT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(20) NOT NULL,
  name VARCHAR(120) NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS sales_products (
  id INT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(20) NULL,
  sku VARCHAR(60) NOT NULL,
  name VARCHAR(180) NOT NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  description VARCHAR(500) NULL,
  barcode VARCHAR(60) NULL,
  unit_price DECIMAL(18,2) NOT NULL,
  cost_price DECIMAL(18,2) NOT NULL DEFAULT 0,
  tax_rate DECIMAL(10,4) NOT NULL,
  image_path VARCHAR(500) NULL,
  color VARCHAR(60) NULL,
  sub_classification_id INT NULL,
  ncm VARCHAR(20) NULL,
  weight DECIMAL(10,3) NOT NULL DEFAULT 0,
  width DECIMAL(10,2) NOT NULL DEFAULT 0,
  height DECIMAL(10,2) NOT NULL DEFAULT 0,
  depth DECIMAL(10,2) NOT NULL DEFAULT 0,
  voltage VARCHAR(20) NULL,
  brand_id INT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY uq_sales_products_code (code),
  UNIQUE KEY uq_sales_products_sku (sku),
  UNIQUE KEY uq_sales_products_barcode (barcode),
  CONSTRAINT fk_product_sub_class FOREIGN KEY (sub_classification_id) REFERENCES product_sub_classifications(id),
  CONSTRAINT fk_product_brand FOREIGN KEY (brand_id) REFERENCES product_brands(id)
);

CREATE TABLE IF NOT EXISTS product_custom_values (
  id INT AUTO_INCREMENT PRIMARY KEY,
  product_id INT NOT NULL,
  field_id INT NOT NULL,
  value VARCHAR(255) NOT NULL DEFAULT '',
  CONSTRAINT fk_cv_product FOREIGN KEY (product_id) REFERENCES sales_products(id),
  CONSTRAINT fk_cv_field FOREIGN KEY (field_id) REFERENCES product_custom_fields(id),
  UNIQUE KEY uq_product_field (product_id, field_id)
);

CREATE TABLE IF NOT EXISTS sales_carriers (
  id INT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(30) NOT NULL DEFAULT '',
  name VARCHAR(160) NOT NULL,
  document VARCHAR(40) NOT NULL DEFAULT '',
  contact_name VARCHAR(120) NULL,
  email VARCHAR(160) NULL,
  phone VARCHAR(30) NULL,
  street VARCHAR(160) NULL,
  number VARCHAR(30) NULL,
  district VARCHAR(100) NULL,
  city VARCHAR(100) NULL,
  state VARCHAR(2) NULL,
  zip_code VARCHAR(20) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS carrier_service_areas (
  id INT AUTO_INCREMENT PRIMARY KEY,
  carrier_id INT NOT NULL,
  municipality_id INT NOT NULL,
  CONSTRAINT fk_csa_carrier FOREIGN KEY (carrier_id) REFERENCES sales_carriers(id),
  CONSTRAINT fk_csa_municipality FOREIGN KEY (municipality_id) REFERENCES municipalities(id),
  UNIQUE KEY uq_carrier_municipality (carrier_id, municipality_id)
);

CREATE TABLE IF NOT EXISTS sales_quotes (
  id INT AUTO_INCREMENT PRIMARY KEY,
  quote_number INT NOT NULL DEFAULT 0,
  customer_id INT NOT NULL,
  carrier_id INT NULL,
  subtotal DECIMAL(18,2) NOT NULL,
  tax_amount DECIMAL(18,2) NOT NULL,
  discount_amount DECIMAL(18,2) NOT NULL,
  freight_amount DECIMAL(18,2) NOT NULL,
  total_amount DECIMAL(18,2) NOT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'ABERTO',
  approved_order_id INT NULL,
  notes VARCHAR(255) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_quote_customer FOREIGN KEY (customer_id) REFERENCES sales_customers(id),
  CONSTRAINT fk_quote_carrier FOREIGN KEY (carrier_id) REFERENCES sales_carriers(id)
);

CREATE TABLE IF NOT EXISTS sales_quote_items (
  id INT AUTO_INCREMENT PRIMARY KEY,
  quote_id INT NOT NULL,
  product_id INT NOT NULL,
  description VARCHAR(220) NOT NULL,
  quantity DECIMAL(18,4) NOT NULL,
  unit_price DECIMAL(18,2) NOT NULL,
  tax_rate DECIMAL(10,4) NOT NULL,
  line_total DECIMAL(18,2) NOT NULL,
  CONSTRAINT fk_quote_item_quote FOREIGN KEY (quote_id) REFERENCES sales_quotes(id),
  CONSTRAINT fk_quote_item_product FOREIGN KEY (product_id) REFERENCES sales_products(id)
);

CREATE TABLE IF NOT EXISTS sales_orders (
  id INT AUTO_INCREMENT PRIMARY KEY,
  order_number INT NOT NULL,
  customer_id INT NOT NULL,
  carrier_id INT NULL,
  subtotal DECIMAL(18,2) NOT NULL,
  tax_amount DECIMAL(18,2) NOT NULL,
  discount_amount DECIMAL(18,2) NOT NULL,
  freight_amount DECIMAL(18,2) NOT NULL,
  freight_type VARCHAR(10) NOT NULL DEFAULT 'CIF',
  total_amount DECIMAL(18,2) NOT NULL,
  payment_term VARCHAR(60) NOT NULL DEFAULT 'A_VISTA',
  status VARCHAR(20) NOT NULL DEFAULT 'ABERTO',
  source_type VARCHAR(20) NOT NULL DEFAULT 'MANUAL',
  source_quote_id INT NULL,
  notes VARCHAR(255) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_order_customer FOREIGN KEY (customer_id) REFERENCES sales_customers(id),
  CONSTRAINT fk_order_carrier FOREIGN KEY (carrier_id) REFERENCES sales_carriers(id)
);

CREATE TABLE IF NOT EXISTS sales_order_items (
  id INT AUTO_INCREMENT PRIMARY KEY,
  order_id INT NOT NULL,
  product_id INT NOT NULL,
  description VARCHAR(220) NOT NULL,
  quantity DECIMAL(18,4) NOT NULL,
  unit_price DECIMAL(18,2) NOT NULL,
  tax_rate DECIMAL(10,4) NOT NULL,
  line_total DECIMAL(18,2) NOT NULL,
  CONSTRAINT fk_order_item_order FOREIGN KEY (order_id) REFERENCES sales_orders(id),
  CONSTRAINT fk_order_item_product FOREIGN KEY (product_id) REFERENCES sales_products(id)
);

CREATE TABLE IF NOT EXISTS sales_invoices (
  id INT AUTO_INCREMENT PRIMARY KEY,
  invoice_number INT NOT NULL,
  order_id INT NULL,
  customer_id INT NULL,
  total_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
  payment_term VARCHAR(60) NOT NULL DEFAULT 'A_VISTA',
  source_type VARCHAR(20) NOT NULL DEFAULT 'PEDIDO',
  status VARCHAR(20) NOT NULL DEFAULT 'GERADA',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY uq_sales_invoices_order_id (order_id),
  CONSTRAINT fk_invoice_order FOREIGN KEY (order_id) REFERENCES sales_orders(id),
  CONSTRAINT fk_invoice_customer FOREIGN KEY (customer_id) REFERENCES sales_customers(id)
);

CREATE TABLE IF NOT EXISTS inventory_suppliers (
  id INT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(30) NOT NULL,
  name VARCHAR(180) NOT NULL,
  tax_id VARCHAR(40) NOT NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  contact_name VARCHAR(120) NULL,
  email VARCHAR(160) NULL,
  phone VARCHAR(30) NULL,
  street VARCHAR(160) NULL,
  number VARCHAR(30) NULL,
  district VARCHAR(100) NULL,
  city VARCHAR(100) NULL,
  state VARCHAR(2) NULL,
  zip_code VARCHAR(20) NULL,
  minimum_order DECIMAL(18,2) NOT NULL DEFAULT 0,
  segment VARCHAR(120) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY uq_inventory_suppliers_code (code),
  UNIQUE KEY uq_inventory_suppliers_tax_id (tax_id)
);

CREATE TABLE IF NOT EXISTS purchase_requests (
  id INT AUTO_INCREMENT PRIMARY KEY,
  product_id INT NOT NULL,
  requested_qty DECIMAL(18,4) NOT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'ABERTA',
  notes VARCHAR(255) NULL,
  supplier_id INT NULL,
  approved_qty DECIMAL(18,4) NULL,
  approved_unit_price DECIMAL(18,2) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  approved_at DATETIME NULL,
  CONSTRAINT fk_pr_product FOREIGN KEY (product_id) REFERENCES sales_products(id),
  CONSTRAINT fk_pr_supplier FOREIGN KEY (supplier_id) REFERENCES inventory_suppliers(id)
);

CREATE TABLE IF NOT EXISTS purchases (
  id INT AUTO_INCREMENT PRIMARY KEY,
  request_id INT NULL,
  supplier_id INT NOT NULL,
  product_id INT NOT NULL,
  quantity DECIMAL(18,4) NOT NULL,
  unit_price DECIMAL(18,2) NOT NULL,
  payment_term VARCHAR(60) NOT NULL DEFAULT 'A_VISTA',
  source_type VARCHAR(20) NOT NULL DEFAULT 'SOLICITACAO',
  status VARCHAR(20) NOT NULL DEFAULT 'PENDENTE_ENTREGA',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  delivered_at DATETIME NULL,
  CONSTRAINT fk_purchase_request FOREIGN KEY (request_id) REFERENCES purchase_requests(id),
  CONSTRAINT fk_purchase_supplier FOREIGN KEY (supplier_id) REFERENCES inventory_suppliers(id),
  CONSTRAINT fk_purchase_product FOREIGN KEY (product_id) REFERENCES sales_products(id)
);

CREATE TABLE IF NOT EXISTS stock_movements (
  id INT AUTO_INCREMENT PRIMARY KEY,
  product_id INT NOT NULL,
  movement_type VARCHAR(20) NOT NULL,
  quantity DECIMAL(18,4) NOT NULL,
  source_type VARCHAR(30) NOT NULL,
  source_id INT NULL,
  notes VARCHAR(255) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_stock_product FOREIGN KEY (product_id) REFERENCES sales_products(id)
);

CREATE TABLE IF NOT EXISTS finance_receivables (
  id INT AUTO_INCREMENT PRIMARY KEY,
  order_id INT NULL,
  invoice_id INT NULL,
  customer_id INT NULL,
  installment_number INT NOT NULL,
  due_date DATE NOT NULL,
  amount DECIMAL(18,2) NOT NULL,
  payment_term VARCHAR(60) NOT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'PENDENTE',
  paid_at DATETIME NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY uq_fin_receivable_invoice_installment (invoice_id, installment_number),
  CONSTRAINT fk_fin_receivable_order FOREIGN KEY (order_id) REFERENCES sales_orders(id),
  CONSTRAINT fk_fin_receivable_invoice FOREIGN KEY (invoice_id) REFERENCES sales_invoices(id),
  CONSTRAINT fk_fin_receivable_customer FOREIGN KEY (customer_id) REFERENCES sales_customers(id)
);

CREATE TABLE IF NOT EXISTS finance_payable_recurring (
  id INT AUTO_INCREMENT PRIMARY KEY,
  supplier_id INT NULL,
  description VARCHAR(180) NOT NULL,
  monthly_amount DECIMAL(18,2) NOT NULL,
  start_date DATE NOT NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  canceled_at DATETIME NULL,
  CONSTRAINT fk_fin_rec_supplier FOREIGN KEY (supplier_id) REFERENCES inventory_suppliers(id)
);

CREATE TABLE IF NOT EXISTS finance_payables (
  id INT AUTO_INCREMENT PRIMARY KEY,
  purchase_id INT NULL,
  supplier_id INT NULL,
  recurring_id INT NULL,
  description VARCHAR(180) NOT NULL,
  installment_number INT NOT NULL,
  due_date DATE NOT NULL,
  amount DECIMAL(18,2) NOT NULL,
  payment_term VARCHAR(60) NOT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'PENDENTE',
  paid_at DATETIME NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_fin_payable_purchase FOREIGN KEY (purchase_id) REFERENCES purchases(id),
  CONSTRAINT fk_fin_payable_supplier FOREIGN KEY (supplier_id) REFERENCES inventory_suppliers(id),
  CONSTRAINT fk_fin_payable_recurring FOREIGN KEY (recurring_id) REFERENCES finance_payable_recurring(id)
);
";

    private readonly MySqlConnectionFactory _connectionFactory;

    public MySqlSchemaInitializer(MySqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var builder = new MySqlConnectionStringBuilder(_connectionFactory.ConnectionString);
        var databaseName = builder.Database;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("A connection string do MySQL precisa informar o nome do banco.");
        }

        var serverBuilder = new MySqlConnectionStringBuilder(_connectionFactory.ConnectionString)
        {
            Database = string.Empty
        };

        await using (var serverConnection = new MySqlConnection(serverBuilder.ConnectionString))
        {
            await serverConnection.OpenAsync(cancellationToken);
            await using var createDatabaseCommand = new MySqlCommand(
                $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;",
                serverConnection);
            await createDatabaseCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using (var command = new MySqlCommand(SchemaSql, connection))
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await EnsureLegacyColumnsAsync(connection, cancellationToken);
    }

    private static async Task EnsureLegacyColumnsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await EnsureColumnAsync(connection, "municipalities", "code", "ALTER TABLE municipalities ADD COLUMN code VARCHAR(30) NOT NULL DEFAULT ''", cancellationToken);

        await EnsureColumnAsync(connection, "sales_customers", "code", "ALTER TABLE sales_customers ADD COLUMN code VARCHAR(30) NOT NULL DEFAULT ''", cancellationToken);
        await EnsureColumnAsync(connection, "sales_customers", "is_active", "ALTER TABLE sales_customers ADD COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1", cancellationToken);
        await EnsureColumnAsync(connection, "sales_customers", "contact_name", "ALTER TABLE sales_customers ADD COLUMN contact_name VARCHAR(120) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_customers", "street", "ALTER TABLE sales_customers ADD COLUMN street VARCHAR(160) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_customers", "number", "ALTER TABLE sales_customers ADD COLUMN number VARCHAR(30) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_customers", "district", "ALTER TABLE sales_customers ADD COLUMN district VARCHAR(100) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_customers", "city", "ALTER TABLE sales_customers ADD COLUMN city VARCHAR(100) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_customers", "state", "ALTER TABLE sales_customers ADD COLUMN state VARCHAR(2) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_customers", "zip_code", "ALTER TABLE sales_customers ADD COLUMN zip_code VARCHAR(20) NULL", cancellationToken);

        await EnsureColumnAsync(connection, "sales_carriers", "code", "ALTER TABLE sales_carriers ADD COLUMN code VARCHAR(30) NOT NULL DEFAULT ''", cancellationToken);
        await EnsureColumnAsync(connection, "sales_carriers", "contact_name", "ALTER TABLE sales_carriers ADD COLUMN contact_name VARCHAR(120) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_carriers", "email", "ALTER TABLE sales_carriers ADD COLUMN email VARCHAR(160) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_carriers", "phone", "ALTER TABLE sales_carriers ADD COLUMN phone VARCHAR(30) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_carriers", "street", "ALTER TABLE sales_carriers ADD COLUMN street VARCHAR(160) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_carriers", "number", "ALTER TABLE sales_carriers ADD COLUMN number VARCHAR(30) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_carriers", "district", "ALTER TABLE sales_carriers ADD COLUMN district VARCHAR(100) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_carriers", "city", "ALTER TABLE sales_carriers ADD COLUMN city VARCHAR(100) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_carriers", "state", "ALTER TABLE sales_carriers ADD COLUMN state VARCHAR(2) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_carriers", "zip_code", "ALTER TABLE sales_carriers ADD COLUMN zip_code VARCHAR(20) NULL", cancellationToken);

        await EnsureColumnAsync(connection, "inventory_suppliers", "code", "ALTER TABLE inventory_suppliers ADD COLUMN code VARCHAR(30) NOT NULL DEFAULT ''", cancellationToken);
        await EnsureColumnAsync(connection, "inventory_suppliers", "tax_id", "ALTER TABLE inventory_suppliers ADD COLUMN tax_id VARCHAR(40) NOT NULL DEFAULT ''", cancellationToken);
        await EnsureColumnAsync(connection, "inventory_suppliers", "is_active", "ALTER TABLE inventory_suppliers ADD COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1", cancellationToken);
        await EnsureColumnAsync(connection, "inventory_suppliers", "contact_name", "ALTER TABLE inventory_suppliers ADD COLUMN contact_name VARCHAR(120) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "inventory_suppliers", "email", "ALTER TABLE inventory_suppliers ADD COLUMN email VARCHAR(160) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "inventory_suppliers", "phone", "ALTER TABLE inventory_suppliers ADD COLUMN phone VARCHAR(30) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "inventory_suppliers", "street", "ALTER TABLE inventory_suppliers ADD COLUMN street VARCHAR(160) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "inventory_suppliers", "number", "ALTER TABLE inventory_suppliers ADD COLUMN number VARCHAR(30) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "inventory_suppliers", "district", "ALTER TABLE inventory_suppliers ADD COLUMN district VARCHAR(100) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "inventory_suppliers", "city", "ALTER TABLE inventory_suppliers ADD COLUMN city VARCHAR(100) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "inventory_suppliers", "state", "ALTER TABLE inventory_suppliers ADD COLUMN state VARCHAR(2) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "inventory_suppliers", "zip_code", "ALTER TABLE inventory_suppliers ADD COLUMN zip_code VARCHAR(20) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "inventory_suppliers", "minimum_order", "ALTER TABLE inventory_suppliers ADD COLUMN minimum_order DECIMAL(18,2) NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "inventory_suppliers", "segment", "ALTER TABLE inventory_suppliers ADD COLUMN segment VARCHAR(120) NULL", cancellationToken);

        await EnsureColumnAsync(connection, "sales_quotes", "quote_number", "ALTER TABLE sales_quotes ADD COLUMN quote_number INT NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "sales_quotes", "status", "ALTER TABLE sales_quotes ADD COLUMN status VARCHAR(20) NOT NULL DEFAULT 'ABERTO'", cancellationToken);
        await EnsureColumnAsync(connection, "sales_quotes", "approved_order_id", "ALTER TABLE sales_quotes ADD COLUMN approved_order_id INT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_quotes", "notes", "ALTER TABLE sales_quotes ADD COLUMN notes VARCHAR(255) NULL", cancellationToken);

        await EnsureColumnAsync(connection, "sales_orders", "status", "ALTER TABLE sales_orders ADD COLUMN status VARCHAR(20) NOT NULL DEFAULT 'ABERTO'", cancellationToken);
        await EnsureColumnAsync(connection, "sales_orders", "source_type", "ALTER TABLE sales_orders ADD COLUMN source_type VARCHAR(20) NOT NULL DEFAULT 'MANUAL'", cancellationToken);
        await EnsureColumnAsync(connection, "sales_orders", "source_quote_id", "ALTER TABLE sales_orders ADD COLUMN source_quote_id INT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_orders", "notes", "ALTER TABLE sales_orders ADD COLUMN notes VARCHAR(255) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_orders", "payment_term", "ALTER TABLE sales_orders ADD COLUMN payment_term VARCHAR(60) NOT NULL DEFAULT 'A_VISTA'", cancellationToken);
        await EnsureColumnAsync(connection, "sales_orders", "freight_type", "ALTER TABLE sales_orders ADD COLUMN freight_type VARCHAR(10) NOT NULL DEFAULT 'CIF'", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "stock_quantity", "ALTER TABLE sales_products ADD COLUMN stock_quantity DECIMAL(18,4) NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "code", "ALTER TABLE sales_products ADD COLUMN code VARCHAR(20) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "is_active", "ALTER TABLE sales_products ADD COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "description", "ALTER TABLE sales_products ADD COLUMN description VARCHAR(500) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "barcode", "ALTER TABLE sales_products ADD COLUMN barcode VARCHAR(60) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "cost_price", "ALTER TABLE sales_products ADD COLUMN cost_price DECIMAL(18,2) NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "image_path", "ALTER TABLE sales_products ADD COLUMN image_path VARCHAR(500) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "color", "ALTER TABLE sales_products ADD COLUMN color VARCHAR(60) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "sub_classification_id", "ALTER TABLE sales_products ADD COLUMN sub_classification_id INT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "ncm", "ALTER TABLE sales_products ADD COLUMN ncm VARCHAR(20) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "weight", "ALTER TABLE sales_products ADD COLUMN weight DECIMAL(10,3) NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "width", "ALTER TABLE sales_products ADD COLUMN width DECIMAL(10,2) NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "height", "ALTER TABLE sales_products ADD COLUMN height DECIMAL(10,2) NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "depth", "ALTER TABLE sales_products ADD COLUMN depth DECIMAL(10,2) NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "sales_products", "voltage", "ALTER TABLE sales_products ADD COLUMN voltage VARCHAR(20) NULL", cancellationToken);
        await EnsureColumnAsync(connection, "purchases", "payment_term", "ALTER TABLE purchases ADD COLUMN payment_term VARCHAR(60) NOT NULL DEFAULT 'A_VISTA'", cancellationToken);
        await EnsureColumnAsync(connection, "purchases", "source_type", "ALTER TABLE purchases ADD COLUMN source_type VARCHAR(20) NOT NULL DEFAULT 'SOLICITACAO'", cancellationToken);
        await EnsureColumnAsync(connection, "finance_receivables", "customer_id", "ALTER TABLE finance_receivables ADD COLUMN customer_id INT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_invoices", "customer_id", "ALTER TABLE sales_invoices ADD COLUMN customer_id INT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "sales_invoices", "total_amount", "ALTER TABLE sales_invoices ADD COLUMN total_amount DECIMAL(18,2) NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "sales_invoices", "payment_term", "ALTER TABLE sales_invoices ADD COLUMN payment_term VARCHAR(60) NOT NULL DEFAULT 'A_VISTA'", cancellationToken);
        await EnsureColumnAsync(connection, "sales_invoices", "source_type", "ALTER TABLE sales_invoices ADD COLUMN source_type VARCHAR(20) NOT NULL DEFAULT 'PEDIDO'", cancellationToken);
        await MakeColumnNullableAsync(connection, "purchases", "request_id", "INT", cancellationToken);
        await MakeColumnNullableAsync(connection, "sales_invoices", "order_id", "INT", cancellationToken);
        await MakeColumnNullableAsync(connection, "finance_receivables", "order_id", "INT", cancellationToken);
        await MakeColumnNullableAsync(connection, "finance_receivables", "invoice_id", "INT", cancellationToken);
        await MakeColumnNullableAsync(connection, "finance_receivables", "customer_id", "INT", cancellationToken);
        await EnsureInternalCodesAsync(connection, "sales_customers", "code", "C", 30, cancellationToken);
        await EnsureInternalCodesAsync(connection, "sales_products", "code", "P", 20, cancellationToken);
        await EnsureInternalCodesAsync(connection, "inventory_suppliers", "code", "F", 30, cancellationToken);
        await EnsureUniqueIndexAsync(connection, "sales_customers", "uq_sales_customers_code", "ALTER TABLE sales_customers ADD UNIQUE KEY uq_sales_customers_code (code)", cancellationToken);
        await EnsureUniqueIndexAsync(connection, "sales_customers", "uq_sales_customers_document", "ALTER TABLE sales_customers ADD UNIQUE KEY uq_sales_customers_document (document)", cancellationToken);
        await EnsureUniqueIndexAsync(connection, "sales_products", "uq_sales_products_code", "ALTER TABLE sales_products ADD UNIQUE KEY uq_sales_products_code (code)", cancellationToken);
        await EnsureUniqueIndexAsync(connection, "sales_products", "uq_sales_products_sku", "ALTER TABLE sales_products ADD UNIQUE KEY uq_sales_products_sku (sku)", cancellationToken);
        await EnsureUniqueIndexAsync(connection, "sales_products", "uq_sales_products_barcode", "ALTER TABLE sales_products ADD UNIQUE KEY uq_sales_products_barcode (barcode)", cancellationToken);
        await EnsureUniqueIndexAsync(connection, "inventory_suppliers", "uq_inventory_suppliers_code", "ALTER TABLE inventory_suppliers ADD UNIQUE KEY uq_inventory_suppliers_code (code)", cancellationToken);
        await EnsureUniqueIndexAsync(connection, "inventory_suppliers", "uq_inventory_suppliers_tax_id", "ALTER TABLE inventory_suppliers ADD UNIQUE KEY uq_inventory_suppliers_tax_id (tax_id)", cancellationToken);
        await EnsureUniqueIndexAsync(connection, "sales_invoices", "uq_sales_invoices_order_id", "ALTER TABLE sales_invoices ADD UNIQUE KEY uq_sales_invoices_order_id (order_id)", cancellationToken);

        // Carrier document unique (allow NULL for carriers without CNPJ in legacy data)
        await MakeColumnNullableAsync(connection, "sales_carriers", "document", "VARCHAR(40)", cancellationToken);
        await CleanupDuplicateCarrierDocumentsAsync(connection, cancellationToken);
        await EnsureUniqueIndexAsync(connection, "sales_carriers", "uq_sales_carriers_document", "ALTER TABLE sales_carriers ADD UNIQUE KEY uq_sales_carriers_document (document)", cancellationToken);

        // Municipality country column + widen state
        await EnsureColumnAsync(connection, "municipalities", "country", "ALTER TABLE municipalities ADD COLUMN country VARCHAR(100) NOT NULL DEFAULT 'Brasil'", cancellationToken);
        await WidenColumnIfNeededAsync(connection, "municipalities", "state", "VARCHAR(60)", 60, cancellationToken);

        // Product brand_id
        await EnsureColumnAsync(connection, "sales_products", "brand_id", "ALTER TABLE sales_products ADD COLUMN brand_id INT NULL", cancellationToken);
    }

    private static async Task EnsureColumnAsync(MySqlConnection connection, string tableName, string columnName, string alterSql, CancellationToken cancellationToken)
    {
        const string existsSql = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = @tableName
  AND COLUMN_NAME = @columnName;";

        await using var existsCommand = new MySqlCommand(existsSql, connection);
        existsCommand.Parameters.AddWithValue("@tableName", tableName);
        existsCommand.Parameters.AddWithValue("@columnName", columnName);

        var existsRaw = await existsCommand.ExecuteScalarAsync(cancellationToken);
        var exists = Convert.ToInt32(existsRaw) > 0;

        if (exists)
        {
            return;
        }

        await using var alterCommand = new MySqlCommand(alterSql, connection);
        await alterCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureUniqueIndexAsync(MySqlConnection connection, string tableName, string indexName, string alterSql, CancellationToken cancellationToken)
    {
        const string existsSql = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = @tableName
  AND INDEX_NAME = @indexName;";

        await using var existsCommand = new MySqlCommand(existsSql, connection);
        existsCommand.Parameters.AddWithValue("@tableName", tableName);
        existsCommand.Parameters.AddWithValue("@indexName", indexName);

        var existsRaw = await existsCommand.ExecuteScalarAsync(cancellationToken);
        var exists = Convert.ToInt32(existsRaw) > 0;
        if (exists)
        {
            return;
        }

        if (string.Equals(tableName, "sales_invoices", StringComparison.OrdinalIgnoreCase)
            && string.Equals(indexName, "uq_sales_invoices_order_id", StringComparison.OrdinalIgnoreCase))
        {
            await CleanupDuplicateInvoicesAsync(connection, cancellationToken);
        }

        await using var alterCommand = new MySqlCommand(alterSql, connection);
        await alterCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MakeColumnNullableAsync(MySqlConnection connection, string tableName, string columnName, string sqlType, CancellationToken cancellationToken)
    {
        const string existsSql = @"
SELECT IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = @tableName
  AND COLUMN_NAME = @columnName;";

        await using var existsCommand = new MySqlCommand(existsSql, connection);
        existsCommand.Parameters.AddWithValue("@tableName", tableName);
        existsCommand.Parameters.AddWithValue("@columnName", columnName);
        await using var reader = await existsCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return;

        var nullable = reader.GetString("IS_NULLABLE");

        if (string.Equals(nullable, "YES", StringComparison.OrdinalIgnoreCase))
            return;

        var defaultClause = BuildDefaultClause(reader, "COLUMN_DEFAULT");
        await reader.CloseAsync();

        await using var alter = new MySqlCommand($"ALTER TABLE {tableName} MODIFY COLUMN {columnName} {sqlType} NULL{defaultClause}", connection);
        await alter.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task CleanupDuplicateInvoicesAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        const string hasDuplicatesSql = @"
SELECT COUNT(*)
FROM (
    SELECT order_id
    FROM sales_invoices
    WHERE order_id IS NOT NULL
    GROUP BY order_id
    HAVING COUNT(*) > 1
) d;";

        await using var hasDuplicatesCommand = new MySqlCommand(hasDuplicatesSql, connection);
        var duplicatesRaw = await hasDuplicatesCommand.ExecuteScalarAsync(cancellationToken);
        var duplicateOrders = Convert.ToInt32(duplicatesRaw);
        if (duplicateOrders <= 0)
        {
            return;
        }

        const string deleteDuplicatesSql = @"
DELETE i1
FROM sales_invoices i1
JOIN sales_invoices i2
  ON i1.order_id = i2.order_id
 AND i1.id > i2.id
WHERE i1.order_id IS NOT NULL;";

        await using var deleteCommand = new MySqlCommand(deleteDuplicatesSql, connection);
        await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task CleanupDuplicateCarrierDocumentsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        // First, set empty-string documents to NULL so they don't conflict with UNIQUE
        const string nullifyEmptySql = "UPDATE sales_carriers SET document = NULL WHERE TRIM(document) = ''";
        await using (var nullifyCmd = new MySqlCommand(nullifyEmptySql, connection))
        {
            await nullifyCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        const string hasDuplicatesSql = @"
SELECT COUNT(*)
FROM (
    SELECT document
    FROM sales_carriers
    WHERE document IS NOT NULL
    GROUP BY document
    HAVING COUNT(*) > 1
) d;";

        await using var hasDuplicatesCommand = new MySqlCommand(hasDuplicatesSql, connection);
        var duplicatesRaw = await hasDuplicatesCommand.ExecuteScalarAsync(cancellationToken);
        var duplicateCount = Convert.ToInt32(duplicatesRaw);
        if (duplicateCount <= 0)
            return;

        const string deleteDuplicatesSql = @"
DELETE c1
FROM sales_carriers c1
JOIN sales_carriers c2
  ON c1.document = c2.document
 AND c1.id > c2.id
WHERE c1.document IS NOT NULL;";

        await using var deleteCommand = new MySqlCommand(deleteDuplicatesSql, connection);
        await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task WidenColumnIfNeededAsync(MySqlConnection connection, string tableName, string columnName, string newType, int minLength, CancellationToken cancellationToken)
    {
        const string lengthSql = @"
SELECT CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = @tableName
  AND COLUMN_NAME = @columnName;";

        await using var cmd = new MySqlCommand(lengthSql, connection);
        cmd.Parameters.AddWithValue("@tableName", tableName);
        cmd.Parameters.AddWithValue("@columnName", columnName);
        var raw = await cmd.ExecuteScalarAsync(cancellationToken);
        var currentLength = raw != null && raw != DBNull.Value ? Convert.ToInt32(raw) : 0;

        if (currentLength >= minLength)
            return;

        await using var alter = new MySqlCommand($"ALTER TABLE {tableName} MODIFY COLUMN {columnName} {newType} NOT NULL", connection);
        await alter.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureInternalCodesAsync(MySqlConnection connection, string tableName, string columnName, string prefix, int totalLength, CancellationToken cancellationToken)
    {
        var valueLength = Math.Max(1, totalLength - prefix.Length);
        var sql = $@"
UPDATE {tableName}
SET {columnName} = CONCAT(@prefix, LPAD(id, @valueLength, '0'))
WHERE {columnName} IS NULL OR TRIM({columnName}) = '';";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@prefix", prefix);
        command.Parameters.AddWithValue("@valueLength", valueLength);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string BuildDefaultClause(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return string.Empty;

        var defaultValue = reader.GetValue(ordinal)?.ToString();
        if (string.IsNullOrWhiteSpace(defaultValue))
            return " DEFAULT ''";

        if (string.Equals(defaultValue, "CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase))
            return " DEFAULT CURRENT_TIMESTAMP";

        var escaped = defaultValue.Replace("'", "''", StringComparison.Ordinal);
        return $" DEFAULT '{escaped}'";
    }
}
