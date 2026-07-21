namespace VSSignalsPPEXLink.packets;

using ProtoBuf;

#nullable disable

[ProtoContract]
public class SetDeviceSingleValuePacket
{
    [ProtoMember(1)]
    public float value;
    [ProtoMember(2)]
    public string byPlayer;
}