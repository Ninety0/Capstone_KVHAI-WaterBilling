using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly NotificationRepository _notification;

        public NotificationViewComponent(NotificationRepository notification)
        {
            _notification = notification;
        }

        public async Task<IViewComponentResult> InvokeAsync(string resident_id)
        {
            var notifList = await _notification.GetNotificationByResident(resident_id);
            var viewModel = new ModelBinding
            {
                NotificationResident = notifList
            };
            return View(viewModel);

        }
    }
}
