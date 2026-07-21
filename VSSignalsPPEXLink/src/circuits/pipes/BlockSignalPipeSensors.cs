using System.Collections.Generic;
using System.Linq;
using ExpandedLib.Helpers;
using ExpandedLib.Registries.Entities;
using PipesAndPowerExpanded.BlockNetworkPipe.BlockEntities;
using PipesAndPowerExpanded.BlockNetworkPipe.Blocks;
using signals.src.signalNetwork;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VSSignalsPPEXLink.circuits.pipes;

/// <summary>
/// Manually-toggled in-line valve on a pipe run. Open, it is a normal pipe node and the run flows
/// through; closed, it severs the run at its cell (see
/// <see cref="BlockEntitySignalValve.IsConnectionBroken"/>). Empty-hand right-click toggles it.
/// </summary>
[BlockRegister]
public class BlockSignalPipeSensors : BlockPipeWithConnection
{
  
  // Cached once - consulted on every placement/neighbour recalc, so it must not allocate per read.
  public override Dictionary<string, string[]> AllowedOrientations { get; } =
    new() {
      {
        "signaltempsensor", ["ns", "we", "ud", "sn", "ew", "du"] 
      },
      {
        "signalpressuresensor", ["ns", "we", "ud", "sn", "ew", "du"] 
      }
    };

  protected override string GetFallbackOrientation(string? type) => "ns";

  public override WorldInteraction[] GetPlacedBlockInteractionHelp(
    IWorldAccessor world,
    BlockSelection selection,
    IPlayer forPlayer
  )
  {
    var baseHelp =
      base.GetPlacedBlockInteractionHelp(world, selection, forPlayer) ?? [];
    return baseHelp;
  }
  
  
}
