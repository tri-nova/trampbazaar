using Microsoft.AspNetCore.Mvc.RazorPages;

namespace trampbazaar.Web.Pages;

public sealed class PaymentCancelModel : PageModel
{
    public Guid? PaymentId { get; private set; }

    public void OnGet(Guid? paymentId)
    {
        PaymentId = paymentId;
    }
}
