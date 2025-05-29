using System.Runtime.Serialization;

namespace Windows.Helper.ScreenControl;
// Credits for https://github.com/immense/Remotely for already doing most of the work. That really helped me saving time on this. I will rebuild the classes on a sooner date.
[DataContract]
public enum WindowsSessionType
{
    Console = 1,
    RDP = 2
}

[DataContract]
public class WindowsSession
{
    [DataMember(Name = "ID")]
    public uint Id { get; set; }
    [DataMember(Name = "Name")]
    public string Name { get; set; } = string.Empty;
    [DataMember(Name = "Type")]
    public WindowsSessionType Type { get; set; }
    [DataMember(Name = "Username")]
    public string Username { get; set; } = string.Empty;
}
