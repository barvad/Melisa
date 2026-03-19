using Microsoft.EntityFrameworkCore;

namespace Practicum.MelisaBot.Ef;

public class MelisaDbContext : DbContext
{
    public DbSet<Chunk> Chunks { get; set; }

    public MelisaDbContext(DbContextOptions<MelisaDbContext> options)
        : base(options)
    { }
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector"); // Создает расширение в БД
        modelBuilder.Entity<Chunk>()
            .Property(b => b.Embedding)
            .HasColumnType("vector(512)"); // Указывает размерность MiniLM

        modelBuilder.Entity<Chunk>()
            .HasKey(b => b.Id);

        modelBuilder.Entity<Chunk>()
            .HasIndex(b => b.Embedding)
            .HasMethod("hnsw") // Быстрый индекс для векторного поиска
            .HasOperators("vector_cosine_ops"); // Используем косинусное сходство


    }
}