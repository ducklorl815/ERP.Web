using ERP.Web.Models.Respository;
using ERP.Web.Service.Service;
using ERP.Web.Utility.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<HomeService>();
builder.Services.AddSingleton<ChartsService>();
builder.Services.AddSingleton<SeatMapService>();
builder.Services.AddSingleton<ExamService>();

builder.Services.AddSingleton<ChartsRespo>();
builder.Services.AddSingleton<SeatMapRespo>();
builder.Services.AddSingleton<ExamRespo>();

builder.Services.Configure<DBList>(builder.Configuration.GetSection("ConnectionStrings"));

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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
