namespace IS.Managed;

public class WatchActionArgs : EventArgs
{
    public WatchActionArgs(string action, string podName)
    {
        Action = action;
        PodName = podName;
    }

    public string Action { get; set; } 
    public string PodName { get; set; }
    public bool Cancel { get; set; }
}