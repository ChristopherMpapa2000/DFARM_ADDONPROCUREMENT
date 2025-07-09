using System.Web.Http;

namespace AddonProject.Helpers
{
    public static class AddonMiddlewareRegistration
    {
        public static void RegisterMessageHandlers(HttpConfiguration config)
        {
            // ลงทะเบียน Message Handler ของ Addon
            config.MessageHandlers.Add(new AddonProject.Handlers.AddonApiVersionRedirectHandler());
        }
    }
}
