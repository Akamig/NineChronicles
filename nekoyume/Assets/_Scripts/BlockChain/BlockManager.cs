using System;
using System.IO;
using System.Net;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Nekoyume.Action;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.BlockChain
{
    public static class BlockManager
    {
          // Editor가 아닌 환경에서 사용할 제네시스 블록의 파일명입니다.
          // 만약 이 값을 수정할 경우 entrypoint.sh도 같이 수정할 필요가 있습니다.
          public const string GenesisBlockName = "genesis-block";

          public static string GenesisBlockPath => BlockPath(GenesisBlockName);

          /// <summary>
          /// 블록은 인코딩하여 파일로 내보냅니다.
          /// </summary>
          /// <param name="path">블록이 저장될 파일경로.</param>
          public static void ExportBlock(
              Block<PolymorphicAction<ActionBase>> block,
              string path)
          {
              byte[] encoded = block.Serialize();
              File.WriteAllBytes(path, encoded);
          }

          /// <summary>
          /// 파일로 부터 블록을 읽어옵니다.
          /// </summary>
          /// <param name="path">블록이 저장되어있는 파일경로.</param>
          /// <returns>읽어들인 블록 객체.</returns>
          public static Block<PolymorphicAction<ActionBase>> ImportBlock(string path)
          {
              if (File.Exists(path))
              {
                  var buffer = File.ReadAllBytes(path);
                  return Block<PolymorphicAction<ActionBase>>.Deserialize(buffer);
              }

              var uri = new Uri(path);
              using (var client = new WebClient())
              {
                  byte[] rawGenesisBlock = client.DownloadData(uri);
                  return Block<PolymorphicAction<ActionBase>>.Deserialize(rawGenesisBlock);
              }
          }

          public static Block<PolymorphicAction<ActionBase>> MineGenesisBlock(
              PendingActivationState[] pendingActivationStates)
          {
              var tableSheets = Game.Game.GetTableCsvAssets();
              string goldDistributionCsvPath = Path.Combine(Application.streamingAssetsPath, "GoldDistribution.csv");
              GoldDistribution[] goldDistributions = GoldDistribution.LoadInDescendingEndBlockOrder(goldDistributionCsvPath);
              return Nekoyume.BlockHelper.MineGenesisBlock(
                  tableSheets,
                  goldDistributions,
                  pendingActivationStates,
                  new AdminState(new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"), 1500000),
                 isActivateAdminAddress: false);
          }

          public static string BlockPath(string filename) => Path.Combine(Application.streamingAssetsPath, filename);

    }
}
