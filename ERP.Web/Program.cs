using ERP.Web.Models.Respository;
using ERP.Web.Models.Respository.ControllerSetting;
using ERP.Web.Models.Respository.Tools;
using ERP.Web.Service.Service;
using ERP.Web.Service.Service.ControllerSetting;
using ERP.Web.Utility.Models;
using ERP.Web.Utility.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Razor 即時編譯
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

// Add services to the container.
builder.Services.AddControllersWithViews();

// 權限服務（Singleton - 使用記憶體快取）
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IPermissionService, PermissionService>();

builder.Services.AddSingleton<ControllerSettingService>();
builder.Services.AddSingleton<HomeService>();
builder.Services.AddSingleton<ChartsService>();
builder.Services.AddSingleton<SeatMapService>();
builder.Services.AddSingleton<ExamService>();
builder.Services.AddSingleton<ToolsService>();

builder.Services.AddSingleton<ControllerSettingRepo>();
builder.Services.AddSingleton<ChartsRespo>();
builder.Services.AddSingleton<SeatMapRespo>();
builder.Services.AddSingleton<ExamRespo>();
builder.Services.AddSingleton<ToolsRespo>();

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

app.UseDeveloperExceptionPage();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.GetFullPath(@"..\ERP.Web.Utility\StaticFiles")),
    RequestPath = "/StaticFiles"
});


app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ExamEnglish}/{action=NewTest}");

app.Run();
