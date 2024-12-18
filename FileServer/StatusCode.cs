namespace FileServer
{
    internal enum StatusCode : byte
    {
        Connect,
        RedirectPort,
        PortRedirected,
        DataSent,
        DataReceived
    }
}
