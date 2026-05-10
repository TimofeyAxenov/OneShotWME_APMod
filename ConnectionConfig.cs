namespace OneShot.Archipelago
{
    public class ConnectionConfig
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 38281;
        public string SlotName { get; set; } = "";
        public string? Password { get; set; } = null;
    }
}
