using System.Text;
using FirstAPI.Contexts;
using FirstAPI.Interfaces;
using FirstAPI.Mappings;
using FirstAPI.Middlewares;
using FirstAPI.Models;
using FirstAPI.Repositories;
using FirstAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// EF Core - SQL Server
builder.Services.AddDbContext<TimeSheetContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Repositories
builder.Services.AddScoped<IRepository<string, User>, Repository<string, User>>();
builder.Services.AddScoped<IRepository<int, Employee>, Repository<int, Employee>>();
builder.Services.AddScoped<IRepository<int, Timesheet>, Repository<int, Timesheet>>();
builder.Services.AddScoped<IRepository<int, LeaveRequest>, Repository<int, LeaveRequest>>();
builder.Services.AddScoped<IRepository<int, Project>, Repository<int, Project>>();
builder.Services.AddScoped<IRepository<int, Attendance>, Repository<int, Attendance>>();
builder.Services.AddScoped<IRepository<int, OvertimeRule>, Repository<int, OvertimeRule>>();

// Services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IOvertimeRuleService, OvertimeRuleService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"]!))
        };
    });

// Authorization
builder.Services.AddAuthorization();

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TimeSheet HR API",
        Version = "v1",
        Description = "Employee Timesheet & HR Management API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TimeSheetContext>();
    db.Database.Migrate();

    // Ensure LeaveBalances table exists (in case migration didn't run)
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='LeaveBalances' AND xtype='U')
        BEGIN
            CREATE TABLE LeaveBalances (
                LeaveBalanceId INT IDENTITY(1,1) PRIMARY KEY,
                EmployeeId INT NOT NULL,
                Year INT NOT NULL,
                CasualTotal INT NOT NULL DEFAULT 10,
                CasualUsed INT NOT NULL DEFAULT 0,
                SickTotal INT NOT NULL DEFAULT 10,
                SickUsed INT NOT NULL DEFAULT 0,
                EarnedTotal INT NOT NULL DEFAULT 15,
                EarnedUsed INT NOT NULL DEFAULT 0,
                MaternityTotal INT NOT NULL DEFAULT 180,
                MaternityUsed INT NOT NULL DEFAULT 0,
                PaternityTotal INT NOT NULL DEFAULT 15,
                PaternityUsed INT NOT NULL DEFAULT 0,
                CONSTRAINT FK_LeaveBalances_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId) ON DELETE CASCADE,
                CONSTRAINT UQ_LeaveBalances_EmpYear UNIQUE (EmployeeId, Year)
            )
        END
    ");

    // Ensure ProjectMembers table exists
    db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProjectMembers' AND xtype='U')
        BEGIN
            CREATE TABLE ProjectMembers (
                ProjectMemberId INT IDENTITY(1,1) PRIMARY KEY,
                ProjectId INT NOT NULL,
                EmployeeId INT NOT NULL,
                AssignedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                CONSTRAINT FK_ProjectMembers_Projects FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId) ON DELETE CASCADE,
                CONSTRAINT FK_ProjectMembers_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId),
                CONSTRAINT UQ_ProjectMembers UNIQUE (ProjectId, EmployeeId)
            )
        END
    ");
}

// Global Exception Handler — must be first
app.UseMiddleware<GlobalExceptionMiddleware>();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
