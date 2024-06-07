namespace TodoApi;

using Api.Todos;
using Microsoft.EntityFrameworkCore;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => this.Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>().HasIndex(p => p.Title);

        base.OnModelCreating(modelBuilder);
    }
}
