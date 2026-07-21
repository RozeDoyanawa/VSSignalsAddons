using Vintagestory.API.Common;

namespace VSSignalsPPEXLink.circuits.pipes;

public interface IBlockEntityKnobInteractable {
    public void OnKnobInteraction(ICoreAPI api, int index);
}