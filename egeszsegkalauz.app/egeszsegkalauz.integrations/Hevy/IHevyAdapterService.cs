using egeszsegkalauz.integrations.Common;

namespace egeszsegkalauz.integrations.Hevy;

public interface IHevyAdapterService : IOAuthAdapter, ICursorSyncAdapter, IWebhookAdapter
{
}
