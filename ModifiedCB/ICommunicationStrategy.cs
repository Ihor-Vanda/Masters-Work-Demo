using ModifiedCB.Settings;

namespace ModifiedCB;

public interface ICommunicationStrategy
{
    Task SendMessage(CommunicationSettings settings);
}
