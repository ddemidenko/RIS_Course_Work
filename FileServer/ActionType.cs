namespace FileServer;

public enum ActionType : byte
{
    Register,
    Authorize,
    GetFiles,
    LoadFiles
}
