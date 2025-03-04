namespace MI_GUI_WinUI.Models
{
    public enum GenerationProgressState
    {
        NotStarted,
        Initializing,
        Loading,
        Tokenizing,
        Encoding,
        InitializingLatents,
        Generating,
        Diffusing,
        Decoding,
        Finalizing,
        Completing,
        Completed,
        Failed,
        Cancelled
    }
}
