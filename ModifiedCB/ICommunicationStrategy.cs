using ModifiedCB.Settings;

namespace ModifiedCB;

public interface ICommunicationStrategy
{
    Task<bool> SendMessage(CommunicationSettings settings);
}
