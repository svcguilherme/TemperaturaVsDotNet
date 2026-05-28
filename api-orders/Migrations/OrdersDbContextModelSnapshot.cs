using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OrdersApi.Data;

#nullable disable

namespace OrdersApi.Migrations;

[DbContext(typeof(OrdersDbContext))]
partial class OrdersDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

        modelBuilder.Entity("OrdersApi.Models.Order", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<int>("OrderNumber").HasColumnType("INTEGER");
            b.Property<string>("TotalValue").IsRequired().HasColumnType("TEXT");
            b.Property<string>("Status").IsRequired().HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            b.Property<DateTime?>("ProcessedAt").HasColumnType("TEXT");
            b.Property<string>("ErrorMessage").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("Status");
            b.HasIndex("OrderNumber").IsUnique();
            b.ToTable("Orders");
        });
#pragma warning restore 612, 618
    }
}
