using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using KVHAI.Routes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register DBConnect as a singleton or scoped service
builder.Services.AddSingleton<DBConnect>();
builder.Services.AddSingleton<Hashing>();
builder.Services.AddTransient<InputSanitize>();
builder.Services.AddTransient(typeof(Pagination<>));

// Register EmployeeRepository as a scoped service
builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<ResidentRepository>();
builder.Services.AddScoped<ImageUploadRepository>();
builder.Services.AddScoped<StreetRepository>();
builder.Services.AddScoped<AddressRepository>();
builder.Services.AddScoped<WaterReadingRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"

    );

ResidentRoute.RegisterRoute(app);
StaffRoute.RegisterRoutes(app);

app.Run();
