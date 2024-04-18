using Microsoft.EntityFrameworkCore;

using var context = new Context();
context.Database.EnsureDeleted();
context.Database.EnsureCreated();

QueryThatCanBeGenerated(context);
QueryThatCanNotBeGenerated(context);

static void QueryThatCanBeGenerated(Context context)
{
    string SearchTag = "Abc";

    // Apply / Repeat Filter on every sub-query
    // This works, but produces a query that is too complex if you use a Contains IN expression (Scaling with every ProductTypeX table you concat, which in our case is around 6)

    var query1 = context.ProductType1s.Select(x => new { x.Product, Type = 1 }).Where(x => x.Product.Tags.Any(y => y.Name == SearchTag));
    var query2 = context.ProductType2s.Select(x => new { x.Product, Type = 2 }).Where(x => x.Product.Tags.Any(y => y.Name == SearchTag));

    var concatQuery = query1.Concat(query2);

    var query = concatQuery.ToQueryString();
}
static void QueryThatCanNotBeGenerated(Context context)
{
    string SearchTag = "Abc";

    // Apply Filter only once after the concatenation of all sub queries
    // This does not work, but would be preferrable to work rather than the upper query

    var query1 = context.ProductType1s.Select(x => new { x.Product, Type = 1 });
    var query2 = context.ProductType2s.Select(x => new { x.Product, Type = 2 });

    var concatQuery = query1.Concat(query2);

    concatQuery = concatQuery.Where(x => x.Product.Tags.Any(y => y.Name == SearchTag));

    var query = concatQuery.ToQueryString();
}


public class Context : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductTag> ProductTags { get; set; }
    public DbSet<ProductType1> ProductType1s { get; set; }
    public DbSet<ProductType2> ProductType2s { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=.;Database=EFCore_QueryGen_Broken;User Id=dev;Password=admin;TrustServerCertificate=True;Trusted_Connection=True");
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasKey(x => x.Id);
        modelBuilder.Entity<Product>().HasMany(x => x.Tags).WithMany();

        modelBuilder.Entity<ProductType1>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.Id);
        modelBuilder.Entity<ProductType2>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.Id);
    }
}

public class Product
{
    public long Id { get; set; }
    public ICollection<ProductTag> Tags { get; set; } = null!;
}

public class ProductType1
{
    public long Id { get; set; }
    public Product Product { get; set; } = null!;
}

public class ProductType2
{
    public long Id { get; set; }
    public Product Product { get; set; } = null!;
}

public class ProductTag
{
    public long Id { get; set; }
    public string? Name { get; set; }
}
