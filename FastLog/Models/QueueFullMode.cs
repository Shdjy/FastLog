namespace FastLog.Models
{
    /// <summary>
    /// Defines behavior when the asynchronous log queue is full.
    /// </summary>
    public enum QueueFullMode
    {
        DropWrite = 0,
        Block = 1,
        DropOldest = 2
    }
}
