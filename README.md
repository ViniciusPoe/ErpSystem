# ERP System

ERP desktop para Windows, em português, que reúne **vendas, estoque/compras e financeiro** em um único aplicativo.
Feito em **C# / .NET 8 (WPF)** com **MySQL**, organizado em camadas e módulos.

## Funcionalidades

**Vendas**
- Cadastros de clientes, produtos, marcas, classificações e subclassificações, campos personalizados, transportadoras e municípios
- Orçamentos, pedidos de venda e notas
- Cálculo de totais e impostos
- Geração de PDF (QuestPDF)

**Estoque e compras**
- Fornecedores, solicitações de compra com aprovação ou rejeição, compras e pedidos de compra
- Entradas e saídas de estoque e saldo por produto

**Financeiro**
- Contas a receber e a pagar, com parcelamento por condição de pagamento
- Despesas recorrentes e baixa de títulos

## Arquitetura

```
src/
├── Erp.Domain                 # Contexto e conceitos centrais
├── Erp.Application            # Serviços de aplicação (casos de uso)
├── Erp.Modules.Sales          # Módulo de vendas
├── Erp.Modules.Inventory      # Módulo de estoque e compras
├── Erp.Modules.Finance        # Módulo financeiro
├── Erp.Modules.Invoicing      # Faturamento
├── Erp.Modules.Service        # Assistência técnica
├── Erp.Infrastructure.MySql   # Repositórios, conexão e criação do schema
└── Erp.Desktop.Wpf            # Interface (WPF, MVVM)
```

Cada módulo define suas entidades e a interface do repositório. A implementação com MySQL fica em `Erp.Infrastructure.MySql`, então o banco pode ser trocado sem mexer nas regras de negócio.

## Como rodar

**Requisitos:** Windows, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) e MySQL 8+.

1. Crie um banco vazio:
   ```sql
   CREATE DATABASE erp_sales CHARACTER SET utf8mb4;
   ```
2. Copie `.env.example` para `.env` e ajuste a connection string:
   ```
   ERP_MYSQL_CONNECTION=Server=localhost;Port=3306;Database=erp_sales;Uid=root;Pwd=sua_senha;Allow User Variables=True;
   ```
   A variável de ambiente `ERP_MYSQL_CONNECTION` também funciona e tem prioridade sobre o `.env`.
3. Execute:
   ```
   dotnet run --project src/Erp.Desktop.Wpf
   ```

As tabelas são criadas automaticamente na primeira execução (`MySqlSchemaInitializer`). Uma referência do schema de vendas está em `database/mysql/sales_schema.sql`.

## Stack

C# · .NET 8 · WPF · MySQL (MySqlConnector) · QuestPDF

## Licença

[MIT](LICENSE)
