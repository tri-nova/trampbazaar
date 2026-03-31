using trampbazaar.AdminWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".TrampBazaar.Admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});
builder.Services.AddHttpClient<AdminApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5136/");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSession();

app.UseRouting();

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isLoginPath = path.StartsWithSegments("/Login", StringComparison.OrdinalIgnoreCase);
    var isStaticAsset = Path.HasExtension(path);
    var isAuthenticated = !string.IsNullOrWhiteSpace(context.Session.GetString("AdminUserName"));

    if (!isAuthenticated && !isLoginPath && !isStaticAsset)
    {
        context.Response.Redirect("/Login");
        return;
    }

    if (isAuthenticated && isLoginPath && HttpMethods.IsGet(context.Request.Method))
    {
        context.Response.Redirect("/");
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
