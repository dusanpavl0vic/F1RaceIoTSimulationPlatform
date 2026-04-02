namespace F1.FeedReplay.Service.Domain.Models;

public enum ReplayRunState
{
    Idle = 0,
    Loaded = 1,
    Running = 2,
    Paused = 3,
    Completed = 4,
    Stopped = 5,
    Faulted = 6
}
