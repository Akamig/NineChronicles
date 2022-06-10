﻿using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatchLogs.Model;
using Nekoyume.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module.Arena.Join;
using UnityEngine;

namespace Nekoyume
{
    public static class ArenaSheetExtensions
    {
        public static bool TryGetCurrentRound(
            this ArenaSheet sheet,
            long blockIndex,
            out ArenaSheet.RoundData current)
        {
            try
            {
                current = sheet.GetRoundByBlockIndex(blockIndex);
                return true;
            }
            catch (InvalidOperationException e)
            {
                Debug.LogException(e);
                current = null;
                return false;
            }
        }

        public static bool TryGetNextRound(
            this ArenaSheet sheet,
            long blockIndex,
            out ArenaSheet.RoundData next)
        {
            try
            {
                var current = sheet.GetRoundByBlockIndex(blockIndex);
                var row = sheet.GetRowByBlockIndex(blockIndex);
                return row.TryGetRound(current.Round + 1, out next);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogException(e);
                next = null;
                return false;
            }
        }

        public static bool TryGetSeasonNumber(
            this ArenaSheet sheet,
            long blockIndex,
            int round,
            out int seasonNumber) =>
            sheet
                .GetRowByBlockIndex(blockIndex)
                .TryGetSeasonNumber(round, out seasonNumber);

        public static bool TryGetSeasonNumber(
            this ArenaSheet.Row row,
            int round,
            out int seasonNumber) =>
            row.Round.TryGetSeasonNumber(round, out seasonNumber);

        public static bool TryGetSeasonNumber(
            this IEnumerable<ArenaSheet.RoundData> roundDataEnumerable,
            int round,
            out int seasonNumber)
        {
            var roundDataArray = roundDataEnumerable as ArenaSheet.RoundData[]
                                 ?? roundDataEnumerable.ToArray();
            var firstRound = roundDataArray.FirstOrDefault();
            if (firstRound is null)
            {
                seasonNumber = 0;
                return false;
            }

            var championshipId = firstRound.ChampionshipId;
            if (roundDataArray.Any(e => e.ChampionshipId != championshipId))
            {
                seasonNumber = 0;
                return false;
            }

            // NOTE: The championship cycles once over four times.
            // And each championship includes three seasons.
            seasonNumber = (championshipId % 4 - 1) * 3;
            foreach (var roundData in roundDataArray)
            {
                if (roundData.ArenaType == ArenaType.Season)
                {
                    seasonNumber++;
                }

                if (roundData.Round == round)
                {
                    return roundData.ArenaType == ArenaType.Season;
                }
            }

            return false;
        }

        public static bool TryGetMedalItemResourceId(
            this ArenaSheet.RoundData roundData,
            out int medalItemId)
        {
            if (roundData.ArenaType == ArenaType.OffSeason)
            {
                medalItemId = 0;
                return false;
            }

            // NOTE: The name of the medal item resource is
            // prepared only for championship id 1.
            medalItemId = ArenaHelper.GetMedalItemId(1, roundData.Round);
            return true;
        }

        public static (long beginning, long end, long current) GetSeasonProgress(
            this ArenaSheet.RoundData roundData,
            long blockIndex) => (
            roundData?.StartBlockIndex ?? 0,
            roundData?.EndBlockIndex ?? 0,
            blockIndex);

        public static int GetChampionshipYear(this ArenaSheet.Row row)
        {
            // row.ChampionshipId
            // 1, 2: 2022
            // 3, 4, 5, 6: 2023
            // 7, 8, 9, 10: 2024
            // ...
            return (row.ChampionshipId - 1) / 4 + 2022;
        }

        public static bool IsChampionshipConditionComplete(
            this ArenaSheet sheet,
            int championshipId,
            AvatarState avatarState) =>
            sheet[championshipId].IsChampionshipConditionComplete(avatarState);

        public static bool IsChampionshipConditionComplete(this ArenaSheet.Row row, AvatarState avatarState)
        {
            var medalTotalCount = ArenaHelper.GetMedalTotalCount(row, avatarState);
            var championshipRound = row.Round[7];
            return medalTotalCount >= championshipRound.RequiredMedalCount;
        }
    }
}
