namespace SMS2WS_SyncAgent
{
    public interface ISyncObject
    {
        string ObjectName { get; }
        string ToXml();
    }
}
