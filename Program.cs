using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

IServiceCollection services = new ServiceCollection();

services.AddAutoMapper(typeof(Program).Assembly);
services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source=testdb"));
services.AddScoped<MyHandler>();
var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
    dbContext.Blogs.AddRange(BlogSeed.Get());
    await dbContext.SaveChangesAsync();
}

using (var scope = serviceProvider.CreateScope())
{
    var handler = scope.ServiceProvider.GetRequiredService<MyHandler>();

    var result = await handler.GetBlogs();

    Console.WriteLine(result.FirstOrDefault());
}

public class MyHandler
{
    private readonly IConfigurationProvider _configurationProvider;
    private readonly AppDbContext _dbContext;

    public MyHandler(IMapper mapper, AppDbContext dbContext)
    {
        _configurationProvider = mapper.ConfigurationProvider;
        _dbContext = dbContext;
    }

    public async Task<List<BlogModel>> GetBlogs()
    {
        // This fails since it's unable to project IEnumerable<T> to ValueList<T>.
        return await _dbContext.Blogs
            .ProjectTo<BlogModel>(_configurationProvider)
            .ToListAsync();
    }
}

public class BlogProfile : Profile
{
    public BlogProfile()
    {
        CreateMap<Post, PostModel>();
        CreateMap<Blog, BlogModel>();
    }
}

public record BlogModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public ValueList<PostModel> Posts { get; set; } = new();
}

public record PostModel
{
    public int Id { get; set; }
    public string? Title { get; set; }
}

public class AppDbContext : DbContext
{
    public DbSet<Blog> Blogs => Set<Blog>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
}

public class Blog
{
    public int Id { get; set; }
    public string? Name { get; set; }

    private readonly List<Post> _posts = new();
    public IEnumerable<Post> Posts => _posts.AsEnumerable();

    public void AddPosts(IEnumerable<Post> posts)
    {
        _posts.AddRange(posts);
    }
}

public class Post
{
    public int Id { get; set; }
    public string? Title { get; set; }
}

public class ValueList<T> : List<T>
{
    public ValueList(IEnumerable<T> collection) : base(collection)
    {
    }

    public ValueList() : base()
    {
    }

    public ValueList(int capacity) : base(capacity)
    {
    }

    public override bool Equals(object? obj)
    {
        // This can be improved. Good enough for this sample.

        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not ValueList<T> list)
        {
            return false;
        }
        else if (list.Count != this.Count)
        {
            return false;
        }
        else
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (!list.Contains(this[0])) return false;
            }
        }

        return true;
    }


    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        for (int i = 0; i < this.Count; i++)
        {
            hashCode.Add(this[i]);
        }

        return hashCode.ToHashCode();
    }
}

public static class ValueListExtensions
{
    public static ValueList<T> ToValueList<T>(this IEnumerable<T> collection)
    {
        return new ValueList<T>(collection);
    }
}

public class BlogSeed
{
    public static List<Blog> Get()
    {
        var output = new List<Blog>();

        var blog1 = new Blog { Name = "Blog 1" };
        blog1.AddPosts(new List<Post> { new Post { Title = "Post 1_1" }, new Post { Title = "Post 1_2" } });
        output.Add(blog1);

        var blog2 = new Blog { Name = "Blog 2" };
        blog2.AddPosts(new List<Post> { new Post { Title = "Post 1_1" }, new Post { Title = "Post 1_2" } });
        output.Add(blog2);

        return output;
    }
}