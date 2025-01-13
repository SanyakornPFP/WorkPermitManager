using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WorkPermitManager.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<Db_WorkPermitManagerModel>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("WorkPermitManagerModelConnectionString")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
  .AddCookie(options =>
  {
      options.Cookie.HttpOnly = true;
      options.ExpireTimeSpan = TimeSpan.FromDays(30);
      options.LoginPath = "/Authenication/LoginResponse";
      options.LogoutPath = "/Authenication/LoginResponse";
      options.Cookie.Name = "_WorkPermitManager";
      options.AccessDeniedPath = "/AccessDenied";
      options.SlidingExpiration = true;
  });

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
app.UseCookiePolicy();
app.UseCors();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapDefaultControllerRoute();


app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=DashBoard}")
    .WithStaticAssets();


app.Run();
