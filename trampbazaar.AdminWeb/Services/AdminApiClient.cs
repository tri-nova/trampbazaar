using System.Net.Http.Json;
using System.Text.Json;
using trampbazaar.Shared.Api;
using trampbazaar.Shared.Contracts;

namespace trampbazaar.AdminWeb.Services;

public sealed class AdminApiClient(HttpClient httpClient)
{
    public async Task<AuthResponseDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            ApiRoutes.AdminAuthLogin,
            new LoginRequestDto
            {
                Email = email,
                Password = password
            },
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken: cancellationToken)
                   ?? new AuthResponseDto { IsSuccess = false, Message = "Beklenmeyen bos cevap." };
        }

        var payload = await response.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken: cancellationToken);
        return payload ?? new AuthResponseDto
        {
            IsSuccess = false,
            Message = "Admin girisi basarisiz."
        };
    }

    public async Task<AdminOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<AdminOverviewDto>(ApiRoutes.AdminOverview, cancellationToken)
           ?? new AdminOverviewDto();

    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<AdminUserDto>>(ApiRoutes.AdminUsers, cancellationToken)
           ?? [];

    public async Task<IReadOnlyList<AdminListingDto>> GetListingsAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<AdminListingDto>>(ApiRoutes.AdminListings, cancellationToken)
           ?? [];

    public async Task<IReadOnlyList<AdminCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<AdminCategoryDto>>(ApiRoutes.AdminCategories, cancellationToken)
           ?? [];

    public async Task<IReadOnlyList<AdminPackageDto>> GetPackagesAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<AdminPackageDto>>(ApiRoutes.AdminPackages, cancellationToken)
           ?? [];

    public async Task<AdminPaymentsDashboardDto> GetPaymentsAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<AdminPaymentsDashboardDto>(ApiRoutes.AdminPayments, cancellationToken)
           ?? new AdminPaymentsDashboardDto();

    public async Task<IReadOnlyList<AdminComplaintDto>> GetComplaintsAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<AdminComplaintDto>>(ApiRoutes.AdminComplaints, cancellationToken)
           ?? [];

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<ConversationSummaryDto>>(ApiRoutes.AdminConversations, cancellationToken)
           ?? [];

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<NotificationDto>>(ApiRoutes.AdminNotifications, cancellationToken)
           ?? [];

    public async Task UpdateUserStatusAsync(Guid userId, string status, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            ApiRoutes.AdminUserStatus(userId),
            new AdminUserStatusUpdateRequest { Status = status },
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdateListingStatusAsync(Guid listingId, string status, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            ApiRoutes.AdminListingStatus(listingId),
            new AdminListingStatusUpdateRequest { Status = status },
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task CreateCategoryAsync(AdminCategoryUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(ApiRoutes.AdminCategories, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdateCategoryAsync(Guid categoryId, AdminCategoryUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync(ApiRoutes.AdminCategoryById(categoryId), request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdateCategoryStatusAsync(Guid categoryId, bool isActive, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            ApiRoutes.AdminCategoryStatus(categoryId),
            new AdminCategoryStatusUpdateRequest { IsActive = isActive },
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task CreatePackageAsync(AdminPackageUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(ApiRoutes.AdminPackages, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdatePackageAsync(Guid packageId, AdminPackageUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync(ApiRoutes.AdminPackageById(packageId), request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdatePackageStatusAsync(Guid packageId, bool isActive, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            ApiRoutes.AdminPackageStatus(packageId),
            new AdminPackageStatusUpdateRequest { IsActive = isActive },
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdateComplaintStatusAsync(Guid complaintId, AdminComplaintStatusUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(ApiRoutes.AdminComplaintStatus(complaintId), request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
    }
}
