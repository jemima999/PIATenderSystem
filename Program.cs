using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PIATenderSystem.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ---------- SEED ADMIN + ROLES + TEST CUSTOMER + SAMPLE TENDERS ----------
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    if (!await roleManager.RoleExistsAsync("Customer"))
        await roleManager.CreateAsync(new IdentityRole("Customer"));

    string adminEmail = "admin@piatender.com";
    string adminPassword = "Admin@123";

    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "PIA Admin",
            Role = "Admin",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }

    string customerEmail = "test@skyaviation.com";
    string customerPassword = "Test@123";

    if (await userManager.FindByEmailAsync(customerEmail) == null)
    {
        var customer = new ApplicationUser
        {
            UserName = customerEmail,
            Email = customerEmail,
            FullName = "Sky Aviation Test User",
            Role = "Customer",
            CompanyName = "Sky Aviation Services",
            EmailConfirmed = true
        };

        var custResult = await userManager.CreateAsync(customer, customerPassword);
        if (custResult.Succeeded)
        {
            await userManager.AddToRoleAsync(customer, "Customer");
        }
    }

    if (!dbContext.Tenders.Any())
    {
        dbContext.Tenders.AddRange(
           new Tender { TenderRefNo = 765, Title = "Exchange of A320-214 landing gear shipsets", Department = "Supply Chain Management", StartDate = DateTime.Today.AddDays(-5), ClosingDate = DateTime.Today.AddDays(15), PdfFileName = "" },
new Tender { TenderRefNo = 764, Title = "Procurement of JET-A1 Fuel", Department = "Supply Chain Management", StartDate = DateTime.Today.AddDays(-10), ClosingDate = DateTime.Today.AddDays(8), PdfFileName = "" },
new Tender { TenderRefNo = 761, Title = "Hiring of cafeteria services for training centre", Department = "Facilities Management Division", StartDate = DateTime.Today.AddDays(-14), ClosingDate = DateTime.Today.AddDays(5), PdfFileName = "" },
new Tender { TenderRefNo = 748, Title = "Office stationery items (Annual)", Department = "Supply Chain Management", StartDate = DateTime.Today.AddDays(-30), ClosingDate = DateTime.Today.AddDays(-2), PdfFileName = "" }
        );
        dbContext.SaveChanges();
    }
}

app.Run();