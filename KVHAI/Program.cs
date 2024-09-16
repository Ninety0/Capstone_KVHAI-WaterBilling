using KVHAI.CustomClass;
using KVHAI.Hubs;
using KVHAI.Interface;
using KVHAI.MiddleWareExtension;
using KVHAI.Models;
using KVHAI.Repository;
using KVHAI.Routes;
using KVHAI.SubscribeSqlDependency;

AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);

var builder = WebApplication.CreateBuilder(args);

//set cookie
builder.Services.AddAuthentication("MyCookieAuth").AddCookie("MyCookieAuth", options =>
{
    options.Cookie.Name = "UserLoginCookie";
    options.LoginPath = "/kvhai/resident/login";
    options.AccessDeniedPath = "/kvhai/error";
});

//Set content root  and web root
builder.Host.UseContentRoot(Directory.GetCurrentDirectory());
builder.WebHost.UseWebRoot("wwwroot");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Register DBConnect as a singleton or scoped service
builder.Services.AddScoped<IEmailSender, EmailService>();
builder.Services.AddSingleton<DBConnect>();
builder.Services.AddSingleton<Hashing>();
builder.Services.AddScoped<SubscribeStreetTableDependency>();
builder.Services.AddScoped<StreetHub>();

builder.Services.AddTransient<LoginRepository>();
builder.Services.AddTransient<InputSanitize>();
builder.Services.AddTransient<WaterBillingFunction>();
builder.Services.AddTransient(typeof(Pagination<>));

// Register EmployeeRepository as a scoped service
builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<ResidentRepository>();
builder.Services.AddScoped<ImageUploadRepository>();
builder.Services.AddScoped<StreetRepository>();
builder.Services.AddScoped<AddressRepository>();
builder.Services.AddScoped<WaterReadingRepository>();
builder.Services.AddScoped<WaterBillRepository>();
builder.Services.AddScoped<RequestDetailsRepository>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"

    );

ResidentRoute.RegisterRoute(app);
StaffRoute.RegisterRoutes(app);

app.UseStreetTableDependency<SubscribeStreetTableDependency>();
app.Run();
