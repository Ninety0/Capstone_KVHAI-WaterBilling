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
})
.AddCookie("AdminCookieAuth", options =>
{
    options.Cookie.Name = "AdminLoginCookie";
    options.LoginPath = "/kvhai/staff/login";
    options.AccessDeniedPath = "/kvhai/admin/error";
});


//Set content root  and web root
builder.Host.UseContentRoot(Directory.GetCurrentDirectory());
builder.WebHost.UseWebRoot("wwwroot");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Frequency of ping messages
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30); // Time to wait for ping response
    options.HandshakeTimeout = TimeSpan.FromSeconds(15); // Time for initial connection
});

// Register DBConnect as a singleton or scoped service
builder.Services.AddSingleton<DBConnect>();
builder.Services.AddSingleton<Hashing>();
builder.Services.AddScoped<IEmailSender, EmailService>();
builder.Services.AddScoped<SubscribeStreetTableDependency>();
builder.Services.AddScoped<SubscribeAnnouncementTableDependency>();
builder.Services.AddScoped<SubscribeNotificationTableDependency>();

builder.Services.AddTransient<StreetHub>();
builder.Services.AddTransient<NotificationHub>();
builder.Services.AddTransient<AnnouncementHub>();
builder.Services.AddTransient<LoginRepository>();
builder.Services.AddTransient<InputSanitize>();
builder.Services.AddTransient<WaterBillingFunction>();
builder.Services.AddTransient(typeof(Pagination<>));

// Register EmployeeRepository as a scoped service
builder.Services.AddScoped<ForecastingRepo>();
builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<ResidentRepository>();
builder.Services.AddScoped<ImageUploadRepository>();
builder.Services.AddScoped<StreetRepository>();
builder.Services.AddScoped<AddressRepository>();
builder.Services.AddScoped<WaterReadingRepository>();
builder.Services.AddScoped<WaterBillRepository>();
builder.Services.AddScoped<RequestDetailsRepository>();
builder.Services.AddScoped<AnnouncementRepository>();
builder.Services.AddScoped<AnnouncementImageRepository>();
builder.Services.AddScoped<HubConnectionRepository>();
builder.Services.AddScoped<NotificationRepository>();
builder.Services.AddScoped<ResidentAddressRepository>();
builder.Services.AddScoped<PaymentRepository>();
builder.Services.AddScoped<ListRepository>();

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

app.UseSqlTableDependency<SubscribeAnnouncementTableDependency>("Data Source=DESKTOP-4UFMKHN\\SQLEXPRESS;Initial Catalog=kvha1;Persist Security Info=True;User ID=kvhai_admin;Password=katarunganvillage;");

app.UseSqlTableDependency<SubscribeStreetTableDependency>("Data Source=DESKTOP-4UFMKHN\\SQLEXPRESS;Initial Catalog=kvha1;Persist Security Info=True;User ID=kvhai_admin;Password=katarunganvillage;");

app.UseSqlTableDependency<SubscribeNotificationTableDependency>("Data Source=DESKTOP-4UFMKHN\\SQLEXPRESS;Initial Catalog=kvha1;Persist Security Info=True;User ID=kvhai_admin;Password=katarunganvillage;");

app.Run();
