
using Security.Jwt;
using Reddit.Repositories;
using Reddit.Model;
using Reddit.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "MainPolicy",
    policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyOrigin()
            .AllowAnyMethod();
    });
});

builder.Services.AddTransient<IPasswordHasher, BasicPasswordHasher>();

builder.Services.AddTransient<RedditContext>();

builder.Services.AddTransient<IUserRepository, UserRepository>();
builder.Services.AddTransient<IRepository<ImageDatum>, ImageRepository>(); 
builder.Services.AddTransient<IPostRepository, PostRepository>();
builder.Services.AddTransient<IGroupRepository, GroupRepository>();
builder.Services.AddTransient<IRepository<Upvote>, UpvoteRepository>();
builder.Services.AddTransient<IRepository<Comment>, CommentRepository>();
builder.Services.AddTransient<IRoleRepository, RoleRepository>();

builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IImageService, ImageService>();


builder.Services.AddTransient<IPasswordProvider>(p =>{
    return new PasswordProvider("senhadificil");
});
builder.Services.AddTransient<IJwtService, JwtService>();



var app = builder.Build();
app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
