using Dommel;
using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;
using MUSIC.STREAMING.WEBSITE.Core.Services;
using MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;
using MySqlConnector;
using MUSIC.STREAMING.WEBSITE.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Cấu hình Dommel: Tự động biến đổi PascalCase -> snake_case
DommelMapper.SetColumnNameResolver(new SnakeCaseColumnNameResolver());
DommelMapper.SetKeyPropertyResolver(new DefaultKeyPropertyResolver());
// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddScoped<IDbConnection>(sp => new MySqlConnection(connectionString));

// Repository & Service 
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMusicService, MusicService>();
builder.Services.AddScoped<ISongRepository, SongRepository>();
builder.Services.AddScoped<IInteractionRepository, InteractionRepository>();
builder.Services.AddScoped<IInteractionService, InteractionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAlbumRepository, AlbumRepository>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();
builder.Services.AddScoped<IPlaylistRepository, PlaylistRepository>();

// Cấu hình JWT 
// Lấy Key từ appsettings.json
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    //Chỉ cho .NET biết cách lấy Token từ Cookie "jwt"
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

// CORS  
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Cho phép Cookie
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    options =>
{
    // Thông tin cơ bản
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Music Streaming API", Version = "v1" });

    // Định nghĩa Security Scheme (Cấu hình nút Authorize)
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token theo cú pháp: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    //  Yêu cầu bảo mật cho toàn bộ API
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
}
);

var app = builder.Build();

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Middleware Authentication 
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();