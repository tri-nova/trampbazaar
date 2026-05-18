using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace trampbazaar.Tests;

public sealed class BrowserSmokeTests
{
    [Fact]
    public async Task WebHomePage_RendersAndNavigatesToListings_InRealBrowser()
    {
        await using var webHost = await TestHostProcess.StartAsync(
            projectPath: "trampbazaar.Web/trampbazaar.Web.csproj",
            workingDirectory: GetSolutionRoot(),
            environmentVariables: new Dictionary<string, string>
            {
                ["Api__BaseUrl"] = "http://127.0.0.1:65001/"
            });

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

        var page = await browser.NewPageAsync();
        await page.GotoAsync(webHost.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "TrampBazaar" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Urun sat, acik artirma kur, takas ve teklif akisini tek yerde yonet." })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".status-banner")).ToContainTextAsync("Veritabani baglantisi su anda kullanilamiyor");

        await page.GetByRole(AriaRole.Link, new() { Name = "Ilanlari Kesfet" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/Listings$"));
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Ilanlar" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WebLogin_RedirectsToAccount_WithAuthenticatedSession_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateWebApiStub();
        await using var webHost = await StartWebHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginWebAsync(page, webHost.BaseUrl);

        await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/$|.*/Index$"));
        await Assertions.Expect(page.GetByText("batu", new() { Exact = true })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Hesabim" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/Account$"));
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Hesabim" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("premium", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Starter Paket", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WebRegister_RedirectsToHome_WithAuthenticatedSession_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateWebRegistrationApiStub();
        await using var webHost = await StartWebHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await page.GotoAsync($"{webHost.BaseUrl}/Register", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await page.GetByLabel("Ad soyad").FillAsync("Demo Gercek");
        await page.GetByLabel("Kullanici adi").FillAsync("demo_gercek");
        await page.GetByLabel("Hesap tipi").SelectOptionAsync("corporate");
        await page.GetByLabel("E-posta").FillAsync("demo_gercek@example.com");
        await page.GetByLabel("Sifre", new() { Exact = true }).FillAsync("Password123!");
        await page.GetByLabel("Sifre tekrar").FillAsync("Password123!");
        await page.GetByRole(AriaRole.Button, new() { Name = "Kayit Ol" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/$|.*/Index$"));
        await Assertions.Expect(page.GetByText("demo_gercek", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WebAccountProfile_CanUpdateContactFields_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateWebAccountProfileApiStub();
        await using var webHost = await StartWebHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginWebAsync(page, webHost.BaseUrl);

        await page.GotoAsync($"{webHost.BaseUrl}/Account", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Ad", Exact = true }).FillAsync("Baturay");
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Soyad", Exact = true }).FillAsync("Oztekin");
        await page.GetByRole(AriaRole.Textbox, new() { Name = "E-posta Adresi", Exact = true }).FillAsync("batu@makparsan.com");
        await page.GetByRole(AriaRole.Button, new() { Name = "Uyelik Bilgilerini Guncelle" }).ClickAsync();

        await Assertions.Expect(page.GetByText("Uyelik ve iletisim bilgileri guncellendi.", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Ad", Exact = true })).ToHaveValueAsync("Baturay");
        await Assertions.Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "E-posta Adresi", Exact = true })).ToHaveValueAsync("batu@makparsan.com");
    }

    [Fact]
    public async Task WebCustomerModules_RenderAndMutate_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateWebCustomerModulesApiStub();
        await using var webHost = await StartWebHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginWebAsync(page, webHost.BaseUrl);

        await page.GotoAsync($"{webHost.BaseUrl}/Orders", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Siparislerim" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("TS0906263", new() { Exact = true })).ToBeVisibleAsync();

        await page.GotoAsync($"{webHost.BaseUrl}/Ledger", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Cari Hareketler" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Retro Kamera siparis borcu", new() { Exact = true })).ToBeVisibleAsync();

        await page.GotoAsync($"{webHost.BaseUrl}/Favorites", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByLabel("Yeni favori ekle").SelectOptionAsync(new[] { "77777777-7777-7777-7777-777777777777" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Favoriye Ekle" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Vintage Pikap", new() { Exact = true })).ToBeVisibleAsync();

        await page.GotoAsync($"{webHost.BaseUrl}/StockAlerts", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByLabel("Ilan").SelectOptionAsync(new[] { "77777777-7777-7777-7777-777777777777" });
        await page.GetByLabel("Not").FillAsync("Firsat olursa haber verin");
        await page.GetByRole(AriaRole.Button, new() { Name = "Alarm Ekle" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Firsat olursa haber verin", new() { Exact = true })).ToBeVisibleAsync();

        await page.GotoAsync($"{webHost.BaseUrl}/PriceAlerts", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByLabel("Ilan").SelectOptionAsync(new[] { "55555555-5555-5555-5555-555555555555" });
        await page.GetByLabel("Hedef Fiyat").FillAsync("2900");
        await page.GetByRole(AriaRole.Button, new() { Name = "Alarm Ekle" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Hedef:", new() { Exact = false })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WebAccountPayment_StartsHostedCheckoutSession_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateWebCustomerModulesApiStub();
        await using var webHost = await StartWebHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginWebAsync(page, webHost.BaseUrl);

        await page.GotoAsync($"{webHost.BaseUrl}/AccountPayment", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Kredi Karti ile Odeme" })).ToBeVisibleAsync();

        var navigation = page.WaitForURLAsync("**/PaymentSuccess?paymentId=*", new PageWaitForURLOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.GetByLabel("Odeme Tutari").FillAsync("500");
        await page.GetByLabel("Aciklama").FillAsync("Cari kapama");
        await page.GetByRole(AriaRole.Button, new() { Name = "Guvenli Odeme Oturumu Baslat" }).ClickAsync();
        await navigation;
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Odeme islemi tamamlandi." })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WebPackages_ShowsLoginCallToAction_WhenAnonymous_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateWebPackagesApiStub();
        await using var webHost = await StartWebHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await page.GotoAsync($"{webHost.BaseUrl}/Packages", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Paketler" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Kurumsal Vitrin", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Satin Almak Icin Giris Yap" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WebPackages_PurchaseRedirectsToPaymentSuccess_WhenAuthenticated_InRealBrowser()
    {
        var paymentId = Guid.NewGuid();
        await using var apiStub = BrowserSmokeTestStubs.CreateWebPurchaseApiStub(paymentId);
        await using var webHost = await StartWebHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginWebAsync(page, webHost.BaseUrl);

        await page.GotoAsync($"{webHost.BaseUrl}/Packages", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        var paymentSuccessNavigation = page.WaitForURLAsync($"**/PaymentSuccess?paymentId={paymentId}", new PageWaitForURLOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.GetByRole(AriaRole.Button, new() { Name = "Paketi Satin Al" }).ClickAsync();
        await paymentSuccessNavigation;

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($".*/PaymentSuccess\\?paymentId={paymentId}$"));
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Odeme islemi tamamlandi." })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText(paymentId.ToString(), new() { Exact = false })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WebCreateListing_RedirectsToListingDetail_InRealBrowser()
    {
        var listingId = Guid.NewGuid();
        await using var apiStub = BrowserSmokeTestStubs.CreateWebListingCreateApiStub(listingId);
        await using var webHost = await StartWebHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginWebAsync(page, webHost.BaseUrl);

        await page.GotoAsync($"{webHost.BaseUrl}/CreateListing", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByLabel("Baslik").FillAsync("Test Ilani");
        await page.GetByLabel("Aciklama").FillAsync("Aciklama metni");
        await page.GetByLabel("Kategori").SelectOptionAsync("elektronik");
        await page.GetByLabel("Satis modu").SelectOptionAsync("direct");
        await page.GetByRole(AriaRole.Spinbutton, new() { Name = "Fiyat" }).First.FillAsync("2500");
        await page.GetByRole(AriaRole.Button, new() { Name = "Ilan Olustur" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex($".*/ListingDetail/{listingId}$"));
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Test Ilani" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WebListingDetail_CanSubmitOffer_AndStartConversation_InRealBrowser()
    {
        var listingId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        await using var apiStub = BrowserSmokeTestStubs.CreateWebMarketplaceWorkflowApiStub(listingId, conversationId);
        await using var webHost = await StartWebHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginWebAsync(page, webHost.BaseUrl);

        await page.GotoAsync($"{webHost.BaseUrl}/ListingDetail/{listingId}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByLabel("Teklif tutari").FillAsync("3000");
        await page.GetByLabel("Teklif notu").FillAsync("Bugun alabilirim.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Teklif Gonder" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Teklif gonderildi.", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".offer-card p").Filter(new() { HasText = "Bugun alabilirim." })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Saticiya Mesaj Gonder" }).ClickAsync();
        await Assertions.Expect(page.GetByText("Konusma hazir: ayse ile iletisim baslatildi.", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WebListingDetail_CanSubmitAuctionBid_InRealBrowser()
    {
        var listingId = Guid.NewGuid();
        await using var apiStub = BrowserSmokeTestStubs.CreateWebAuctionWorkflowApiStub(listingId);
        await using var webHost = await StartWebHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginWebAsync(page, webHost.BaseUrl);

        await page.GotoAsync($"{webHost.BaseUrl}/ListingDetail/{listingId}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByLabel("Teklif tutari").FillAsync("1800");
        await page.GetByRole(AriaRole.Button, new() { Name = "Acik Artirmaya Katil" }).ClickAsync();

        await Assertions.Expect(page.GetByText("Acik artirma teklifi gonderildi.", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".auction-bid-card strong").Filter(new() { HasText = "batu" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WebConversationDetail_CanSendMessage_InRealBrowser()
    {
        var listingId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        await using var apiStub = BrowserSmokeTestStubs.CreateWebMarketplaceWorkflowApiStub(listingId, conversationId);
        await using var webHost = await StartWebHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginWebAsync(page, webHost.BaseUrl);

        await page.GotoAsync($"{webHost.BaseUrl}/Conversations", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByRole(AriaRole.Link, new() { Name = "Retro Kamera" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex($".*/ConversationDetail/{conversationId}$"));

        await page.GetByLabel("Yeni mesaj").FillAsync("Yarin teslim alabilirim.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Mesaj Gonder" }).ClickAsync();

        await Assertions.Expect(page.Locator(".message-card p").Filter(new() { HasText = "Yarin teslim alabilirim." })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WebNotifications_CanMarkRead_InRealBrowser()
    {
        var listingId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        await using var apiStub = BrowserSmokeTestStubs.CreateWebMarketplaceWorkflowApiStub(listingId, conversationId);
        await using var webHost = await StartWebHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginWebAsync(page, webHost.BaseUrl);

        await page.GotoAsync($"{webHost.BaseUrl}/Notifications", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Okundu Isaretle" })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Okundu Isaretle" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Okundu Isaretle" })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task WebComplaint_SubmitsAndRedirectsToNotifications_InRealBrowser()
    {
        var listingId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        await using var apiStub = BrowserSmokeTestStubs.CreateWebMarketplaceWorkflowApiStub(listingId, conversationId);
        await using var webHost = await StartWebHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginWebAsync(page, webHost.BaseUrl);

        await page.GotoAsync($"{webHost.BaseUrl}/Complaint?targetEntityType=listing&targetEntityId={listingId}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByLabel("Konu").FillAsync("Yaniltici aciklama");
        await page.GetByLabel("Aciklama").FillAsync("Fotograf ile gelen urun ayni degil.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Sikayet Gonder" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/Notifications$"));
        await Assertions.Expect(page.GetByText("Sikayet alindi", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AdminRoot_RedirectsToLogin_AndLoginFormIsVisible_InRealBrowser()
    {
        await using var adminHost = await TestHostProcess.StartAsync(
            projectPath: "trampbazaar.AdminWeb/trampbazaar.AdminWeb.csproj",
            workingDirectory: GetSolutionRoot(),
            environmentVariables: new Dictionary<string, string>
            {
                ["Api__BaseUrl"] = "http://127.0.0.1:65001/"
            });

        await using var session = await LaunchBrowserAsync();
        var page = await session.Browser.NewPageAsync();
        await page.GotoAsync($"{adminHost.BaseUrl}/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/Login$"));
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Yonetim paneli girisi" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByLabel("E-posta")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByLabel("Sifre")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AdminLogin_RedirectsToUsers_WithAuthenticatedSession_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateAdminApiStub();
        await using var adminHost = await StartAdminHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginAdminAsync(page, adminHost.BaseUrl);

        await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/$|.*/Index$"));
        await Assertions.Expect(page.GetByText("superadmin", new() { Exact = true })).ToBeVisibleAsync();

        await page.Locator(".sidebar-nav").GetByRole(AriaRole.Link, new() { Name = "Kullanicilar" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(new Regex(".*/Users$"));
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Kullanicilar" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("batu@example.com", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AdminUsers_ToggleStatus_ShowsSuccessMessage_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateAdminModerationApiStub();
        await using var adminHost = await StartAdminHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginAdminAsync(page, adminHost.BaseUrl);

        await page.GotoAsync($"{adminHost.BaseUrl}/Users", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByRole(AriaRole.Button, new() { Name = "Pasife Al" }).ClickAsync();

        await Assertions.Expect(page.GetByText("Kullanici durumu guncellendi.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Aktif Et" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AdminCategories_CreateCategory_ShowsCreatedRow_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateAdminCategoryApiStub();
        await using var adminHost = await StartAdminHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginAdminAsync(page, adminHost.BaseUrl);

        await page.GotoAsync($"{adminHost.BaseUrl}/Categories", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("form.category-create-grid").GetByLabel("Ad").FillAsync("Bisiklet");
        await page.Locator("form.category-create-grid").GetByLabel("Slug").FillAsync("bisiklet");
        await page.Locator("form.category-create-grid").GetByLabel("Sira").FillAsync("5");
        await page.GetByRole(AriaRole.Button, new() { Name = "Kategori Ekle" }).ClickAsync();

        await Assertions.Expect(page.GetByText("Kategori olusturuldu.")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("input[value='Bisiklet']")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("input[value='bisiklet']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AdminListings_ToggleStatus_ShowsSuccessMessage_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateAdminOperationsApiStub();
        await using var adminHost = await StartAdminHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginAdminAsync(page, adminHost.BaseUrl);

        await page.GotoAsync($"{adminHost.BaseUrl}/Listings", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByRole(AriaRole.Button, new() { Name = "Yayini Durdur" }).ClickAsync();

        await Assertions.Expect(page.GetByText("Ilan durumu guncellendi.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Yayina Al" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AdminPackages_CreateAndToggleStatus_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateAdminOperationsApiStub();
        await using var adminHost = await StartAdminHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginAdminAsync(page, adminHost.BaseUrl);

        await page.GotoAsync($"{adminHost.BaseUrl}/Packages", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("form.package-create-grid").GetByLabel("Paket tipi").SelectOptionAsync("featured");
        await page.Locator("form.package-create-grid").GetByLabel("Paket adi").FillAsync("One Cikarma");
        await page.Locator("form.package-create-grid").GetByLabel("Fiyat").FillAsync("899");
        await page.Locator("form.package-create-grid").GetByLabel("Sure gun").FillAsync("14");
        await page.Locator("form.package-create-grid").GetByLabel("Ilan kotasi").FillAsync("1");
        await page.GetByRole(AriaRole.Button, new() { Name = "Paket Ekle" }).ClickAsync();

        await Assertions.Expect(page.GetByText("Paket olusturuldu.")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("input[value='One Cikarma']")).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Pasife Al" }).First.ClickAsync();
        await Assertions.Expect(page.GetByText("Paket durumu guncellendi.")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Aktif Et" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AdminComplaints_UpdateStatus_ShowsSuccessMessage_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateAdminOperationsApiStub();
        await using var adminHost = await StartAdminHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginAdminAsync(page, adminHost.BaseUrl);

        await page.GotoAsync($"{adminHost.BaseUrl}/Complaints", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("select[name='status']").SelectOptionAsync("resolved");
        await page.GetByRole(AriaRole.Button, new() { Name = "Kaydet" }).ClickAsync();

        await Assertions.Expect(page.GetByText("Sikayet durumu guncellendi.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AdminPayments_RendersDashboard_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateAdminOperationsApiStub();
        await using var adminHost = await StartAdminHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginAdminAsync(page, adminHost.BaseUrl);

        await page.GotoAsync($"{adminHost.BaseUrl}/Payments", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Odemeler" })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".stat-card").First).ToContainTextAsync("TRY");
        await Assertions.Expect(page.GetByText("Starter Paket", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AdminNotifications_RendersRows_InRealBrowser()
    {
        await using var apiStub = BrowserSmokeTestStubs.CreateAdminOperationsApiStub();
        await using var adminHost = await StartAdminHostAsync(apiStub.BaseUrl);
        await using var session = await LaunchBrowserAsync();

        var page = await session.Browser.NewPageAsync();
        await LoginAdminAsync(page, adminHost.BaseUrl);

        await page.GotoAsync($"{adminHost.BaseUrl}/Notifications", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Son Bildirimler" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Yeni sikayet", new() { Exact = true })).ToBeVisibleAsync();
    }

    private static string GetSolutionRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    private static Task<TestHostProcess> StartWebHostAsync(string apiBaseUrl)
    {
        return TestHostProcess.StartAsync(
            projectPath: "trampbazaar.Web/trampbazaar.Web.csproj",
            workingDirectory: GetSolutionRoot(),
            environmentVariables: new Dictionary<string, string>
            {
                ["Api__BaseUrl"] = $"{apiBaseUrl}/"
            });
    }

    private static Task<TestHostProcess> StartAdminHostAsync(string apiBaseUrl)
    {
        return TestHostProcess.StartAsync(
            projectPath: "trampbazaar.AdminWeb/trampbazaar.AdminWeb.csproj",
            workingDirectory: GetSolutionRoot(),
            environmentVariables: new Dictionary<string, string>
            {
                ["Api__BaseUrl"] = $"{apiBaseUrl}/"
            });
    }

    private static async Task<BrowserSession> LaunchBrowserAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        return new BrowserSession(playwright, browser);
    }

    private static async Task LoginWebAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/Login", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByLabel("E-posta").FillAsync("batu@example.com");
        await page.GetByLabel("Sifre").FillAsync("Password123!");
        await page.GetByRole(AriaRole.Button, new() { Name = "Giris Yap" }).ClickAsync();
    }

    private static async Task LoginAdminAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync($"{baseUrl}/Login", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByLabel("E-posta").FillAsync("admin@example.com");
        await page.GetByLabel("Sifre").FillAsync("Password123!");
        await page.GetByRole(AriaRole.Button, new() { Name = "Giris Yap" }).ClickAsync();
    }

    private sealed class BrowserSession(IPlaywright playwright, IBrowser browser) : IAsyncDisposable
    {
        public IBrowser Browser { get; } = browser;

        public async ValueTask DisposeAsync()
        {
            await Browser.DisposeAsync();
            playwright.Dispose();
        }
    }
}
