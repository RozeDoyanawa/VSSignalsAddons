using Vintagestory.API.Datastructures;

namespace VSSignalsPPEXLink.circuits.pipes;

public class SelectionKnob : RotatableCube {
    public int Index;

    public SelectionKnob(int index, float MinX, float MinY, float MinZ, float MaxX, float MaxY, float MaxZ) : base(MinX, MinY, MinZ, MaxX, MaxY, MaxZ)
    {
        Index = index;
    }
}