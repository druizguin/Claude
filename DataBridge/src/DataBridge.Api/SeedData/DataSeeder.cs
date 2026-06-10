using DataBridge.Connectors.CSV;
using DataBridge.Connectors.Excel;
using DataBridge.Connectors.SQLite;
using DataBridge.Domain.Entities;

namespace DataBridge.Api.SeedData;

public static class DataSeeder
{
    public static async Task SeedAsync(
        ExcelConnector<Product> products,
        SQLiteConnector<User> users,
        CsvConnector<Purchase> purchases,
        CsvConnector<Address> addresses)
    {
        await SeedAddressesAsync(addresses);
        await SeedProductsAsync(products);
        await SeedUsersAsync(users);
        await SeedPurchasesAsync(purchases, users, products);
    }

    private static async Task SeedAddressesAsync(CsvConnector<Address> connector)
    {
        var spec = new Core.Models.QuerySpec { From = "addresses" };
        var existing = await connector.QueryAsync(spec);
        if (existing.TotalCount > 0) return;

        var addressList = new[]
        {
            new Address { Id = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000001"), Street = "123 Main St",     City = "New York",    ZipCode = "10001", Country = "USA"    },
            new Address { Id = Guid.Parse("aaaaaaaa-0002-0000-0000-000000000002"), Street = "45 Oxford St",    City = "London",      ZipCode = "W1A 1AA",Country = "UK"    },
            new Address { Id = Guid.Parse("aaaaaaaa-0003-0000-0000-000000000003"), Street = "789 Yonge St",    City = "Toronto",     ZipCode = "M4W 2G8",Country = "Canada"},
            new Address { Id = Guid.Parse("aaaaaaaa-0004-0000-0000-000000000004"), Street = "22 Elm Street",   City = "Chicago",     ZipCode = "60601",  Country = "USA"   },
            new Address { Id = Guid.Parse("aaaaaaaa-0005-0000-0000-000000000005"), Street = "8 Baker Street",  City = "Manchester",  ZipCode = "M1 1AE", Country = "UK"    },
        };

        foreach (var a in addressList)
            await connector.InsertAsync(a);
    }

    private static async Task SeedProductsAsync(ExcelConnector<Product> connector)
    {
        var spec = new Core.Models.QuerySpec { From = "products" };
        var existing = await connector.QueryAsync(spec);
        if (existing.TotalCount > 0) return;

        var productList = new[]
        {
            // Fruits
            new Product { Id = Guid.NewGuid(), Name = "Apple",      Category = "Fruits",     Price = 1.20m, StockQuantity = 200, Barcode = "0000000000001" },
            new Product { Id = Guid.NewGuid(), Name = "Banana",     Category = "Fruits",     Price = 0.80m, StockQuantity = 300, Barcode = "0000000000002" },
            new Product { Id = Guid.NewGuid(), Name = "Orange",     Category = "Fruits",     Price = 1.50m, StockQuantity = 150, Barcode = "0000000000003" },
            new Product { Id = Guid.NewGuid(), Name = "Mango",      Category = "Fruits",     Price = 2.00m, StockQuantity = 100, Barcode = "0000000000004" },
            new Product { Id = Guid.NewGuid(), Name = "Strawberry", Category = "Fruits",     Price = 3.50m, StockQuantity = 80,  Barcode = "0000000000005" },
            // Vegetables
            new Product { Id = Guid.NewGuid(), Name = "Carrot",     Category = "Vegetables", Price = 0.90m, StockQuantity = 180, Barcode = "0000000000006" },
            new Product { Id = Guid.NewGuid(), Name = "Broccoli",   Category = "Vegetables", Price = 2.20m, StockQuantity = 120, Barcode = "0000000000007" },
            new Product { Id = Guid.NewGuid(), Name = "Spinach",    Category = "Vegetables", Price = 1.80m, StockQuantity = 90,  Barcode = "0000000000008" },
            new Product { Id = Guid.NewGuid(), Name = "Tomato",     Category = "Vegetables", Price = 1.40m, StockQuantity = 250, Barcode = "0000000000009" },
            new Product { Id = Guid.NewGuid(), Name = "Cucumber",   Category = "Vegetables", Price = 1.10m, StockQuantity = 160, Barcode = "0000000000010" },
            // Dairy
            new Product { Id = Guid.NewGuid(), Name = "Milk",       Category = "Dairy",      Price = 2.50m, StockQuantity = 400, Barcode = "0000000000011" },
            new Product { Id = Guid.NewGuid(), Name = "Yogurt",     Category = "Dairy",      Price = 1.90m, StockQuantity = 220, Barcode = "0000000000012" },
            new Product { Id = Guid.NewGuid(), Name = "Cheese",     Category = "Dairy",      Price = 4.50m, StockQuantity = 110, Barcode = "0000000000013" },
            new Product { Id = Guid.NewGuid(), Name = "Butter",     Category = "Dairy",      Price = 3.20m, StockQuantity = 130, Barcode = "0000000000014" },
            new Product { Id = Guid.NewGuid(), Name = "Eggs",       Category = "Dairy",      Price = 3.80m, StockQuantity = 350, Barcode = "0000000000015" },
            // Bakery
            new Product { Id = Guid.NewGuid(), Name = "Bread",      Category = "Bakery",     Price = 2.80m, StockQuantity = 500, Barcode = "0000000000016" },
            new Product { Id = Guid.NewGuid(), Name = "Bagel",      Category = "Bakery",     Price = 1.60m, StockQuantity = 200, Barcode = "0000000000017" },
            new Product { Id = Guid.NewGuid(), Name = "Croissant",  Category = "Bakery",     Price = 2.10m, StockQuantity = 180, Barcode = "0000000000018" },
            new Product { Id = Guid.NewGuid(), Name = "Muffin",     Category = "Bakery",     Price = 1.70m, StockQuantity = 160, Barcode = "0000000000019" },
            new Product { Id = Guid.NewGuid(), Name = "Cookie",     Category = "Bakery",     Price = 0.90m, StockQuantity = 300, Barcode = "0000000000020" },
            // Beverages
            new Product { Id = Guid.NewGuid(), Name = "Water",      Category = "Beverages",  Price = 0.60m, StockQuantity = 600, Barcode = "0000000000021" },
            new Product { Id = Guid.NewGuid(), Name = "Juice",      Category = "Beverages",  Price = 2.90m, StockQuantity = 250, Barcode = "0000000000022" },
            new Product { Id = Guid.NewGuid(), Name = "Soda",       Category = "Beverages",  Price = 1.50m, StockQuantity = 400, Barcode = "0000000000023" },
            new Product { Id = Guid.NewGuid(), Name = "Coffee",     Category = "Beverages",  Price = 4.20m, StockQuantity = 300, Barcode = "0000000000024" },
            new Product { Id = Guid.NewGuid(), Name = "Tea",        Category = "Beverages",  Price = 3.00m, StockQuantity = 280, Barcode = "0000000000025" },
        };

        foreach (var p in productList)
            await connector.InsertAsync(p);

        connector.FlushToDisk();
    }

    private static async Task SeedUsersAsync(SQLiteConnector<User> connector)
    {
        var spec = new Core.Models.QuerySpec { From = "users" };
        var existing = await connector.QueryAsync(spec);
        if (existing.TotalCount > 0) return;

        var userList = new[]
        {
            new User { Id = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000001"), Name = "Alice Johnson", Email = "alice@example.com", Age = 28, Country = "USA",    Status = "active",  SignupDate = new DateTime(2023, 1, 15), AddressPrincipalId = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000001") },
            new User { Id = Guid.Parse("bbbbbbbb-0002-0000-0000-000000000002"), Name = "Bob Smith",    Email = "bob@example.com",   Age = 35, Country = "UK",     Status = "active",  SignupDate = new DateTime(2022, 8, 3),  AddressPrincipalId = Guid.Parse("aaaaaaaa-0002-0000-0000-000000000002") },
            new User { Id = Guid.Parse("bbbbbbbb-0003-0000-0000-000000000003"), Name = "Carol White",  Email = "carol@example.com", Age = 22, Country = "USA",    Status = "pending", SignupDate = new DateTime(2023, 5, 22), AddressPrincipalId = Guid.Parse("aaaaaaaa-0004-0000-0000-000000000004") },
            new User { Id = Guid.Parse("bbbbbbbb-0004-0000-0000-000000000004"), Name = "David Brown",  Email = "david@example.com", Age = 45, Country = "Canada", Status = "active",  SignupDate = new DateTime(2021, 11, 1), AddressPrincipalId = Guid.Parse("aaaaaaaa-0003-0000-0000-000000000003") },
            new User { Id = Guid.Parse("bbbbbbbb-0005-0000-0000-000000000005"), Name = "Eve Davis",    Email = "eve@example.com",   Age = 31, Country = "USA",    Status = "active",  SignupDate = new DateTime(2022, 3, 14), AddressPrincipalId = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000001") },
        };

        foreach (var u in userList)
            await connector.InsertAsync(u);
    }

    private static async Task SeedPurchasesAsync(
        CsvConnector<Purchase> connector,
        SQLiteConnector<User> users,
        ExcelConnector<Product> products)
    {
        var spec = new Core.Models.QuerySpec { From = "purchases" };
        var existing = await connector.QueryAsync(spec);
        if (existing.TotalCount > 0) return;

        var userResult    = await users.QueryAsync(new Core.Models.QuerySpec { From = "users" });
        var productResult = await products.QueryAsync(new Core.Models.QuerySpec { From = "products" });

        var userIds    = userResult.Items.Select(u => u.Id).ToArray();
        var productItems = productResult.Items.ToArray();

        if (userIds.Length == 0 || productItems.Length == 0) return;

        var random = new Random(42);
        var purchaseList = new List<Purchase>();

        for (int i = 0; i < 30; i++)
        {
            var product  = productItems[random.Next(productItems.Length)];
            var qty      = random.Next(1, 6);
            purchaseList.Add(new Purchase
            {
                Id           = Guid.NewGuid(),
                UserId       = userIds[random.Next(userIds.Length)],
                ProductId    = product.Id,
                Quantity     = qty,
                TotalPrice   = product.Price * qty,
                PurchaseDate = DateTime.UtcNow.AddDays(-random.Next(0, 90)),
                Status       = random.Next(3) == 0 ? "pending" : "completed"
            });
        }

        foreach (var p in purchaseList)
            await connector.InsertAsync(p);
    }
}
