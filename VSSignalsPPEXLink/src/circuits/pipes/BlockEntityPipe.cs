using System;
using System.Text;
using ExpandedLib.Blocks.Networks;
using ExpandedLib.Helpers;
using ExpandedLib.Registries.Entities;
using PipesAndPowerExpanded.BlockNetworkPipe;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using PipesAndPowerExpanded.Helpers;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VSSignalsPPEXLink.circuits.pipes;

/// <summary>
/// Block entity for all pipe blocks. Implements <see cref="IPipeNode"/> by delegating to the
/// owning <see cref="PipeNetwork"/> - the BE holds no network state and never broadcasts directly.
/// </summary>
[BlockEntityRegister]
public class BlockEntityPipeSensor : BlockEntityPipe
{
    public WorldInteraction[] GetEntityInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        return [];
    }
}
