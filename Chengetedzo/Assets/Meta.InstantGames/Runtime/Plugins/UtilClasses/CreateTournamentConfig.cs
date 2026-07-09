// Copyright (c) Facebook, Inc. and its affiliates. All rights reserved.
//
// The examples provided by Facebook are for non-commercial testing and evaluation
// purposes only. Facebook reserves all rights not expressly granted.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// FACEBOOK BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;

namespace Meta.InstantGames
{
    public class CreateTournamentConfig
    {
        public string title; // string? Optional text title for the tournament, please do not include user names in title.
        public bool forceScoreValidation; // boolean? Optional boolean that specifies if the tournament requires game server validation before a score can be added to or updated on the leaderboard.
        public string image; // string? Optional base64 encoded image that will be associated with the tournament and included in posts sharing the tournament
        public TournamentSortOrder sortOrder = TournamentSortOrder.HIGHER_IS_BETTER; // string? Optional input for the ordering of which score is best in the tournament. The options are 'HIGHER_IS_BETTER' or 'LOWER_IS_BETTER'. If not specified, the default is 'HIGHER_IS_BETTER'.
        public TournamentScoreFormat scoreFormat = TournamentScoreFormat.NUMERIC; // string? Optional input for the formatting of the scores in the tournament leaderboard. The options are 'NUMERIC' or 'TIME'. If not specified, the default is 'NUMERIC'.
        public long endTime; // number? Optional input for setting a custom end time for the tournament. The number passed in represents a unix timestamp. If not specified, the tournament will end one week after creation.
        public bool forceScoreRangeValidation; // boolean? Optional boolean that specifies if the tournament should use score range validation. If true, then minimum and/or maximum scores should be provided; scores falling outside the range will be automatically rejected. If either minimum or maximum is null, then that side of the range will be ignored.
        public float minimumScore; // number? Optional input will only be used if forceScoreRangeValidation is true. If it is, scores below this will be automatically rejected. If null or forceScoreRangeValidation is false, no minimum will be used
        public float maximumScore; // number? Optional input will only be used if forceScoreRangeValidation is true. If it is, scores above this will be automatically rejected. If null or forceScoreRangeValidation is false, no maximum will be used
        public TournamentType tournamentType; // string? Optional input for tournament type. This can be set to either "COLLABORATIVE" (everyone works together for a goal) or "DEEP" (the standard tournaments). This will default to "DEEP" if no value is provided.
        public int goal; // integer? Input for collaborative tournaments, participants strive to reach this goal to beat the tournament. This input is optional for standard tournaments and required for collaborative tournaments.
        public Json ToJson()
        {
            var json = Json.Object()
                        .Prop("forceScoreValidation", forceScoreValidation)
                        .Prop("sortOrder", sortOrder.ToValue())
                        .Prop("scoreFormat", scoreFormat.ToValue())
                        .Prop("endTime", endTime)
                        .Prop("forceScoreRangeValidation", forceScoreRangeValidation)
                        .Prop("minimumScore", minimumScore)
                        .Prop("maximumScore", maximumScore)
                        .Prop("tournamentType", tournamentType.ToValue())
                        .Prop("goal", goal);
            if (title != null) json.Prop("title", title);
            if (image != null) json.Prop("image", image);
            return json;
        }
    }

    public enum TournamentType
    {
        COLLABORATIVE,
        DEEP,
        DEFAULT
    }

    public static class TournamentTypeExtensions
    {
        public static string ToValue(this TournamentType type)
        {
            return type switch
            {
                TournamentType.COLLABORATIVE => "COLLABORATIVE",
                TournamentType.DEEP => "DEEP",
                TournamentType.DEFAULT => "DEFAULT",
                _ => type.ToString()
            };
        }
    }

    public enum TournamentScoreFormat
    {
        NUMERIC,
        TIME
    }

    public static class TournamentScoreFormatExtensions
    {
        public static string ToValue(this TournamentScoreFormat format)
        {
            return format switch
            {
                TournamentScoreFormat.NUMERIC => "NUMERIC",
                TournamentScoreFormat.TIME => "TIME",
                _ => format.ToString()
            };
        }
    }

    public enum TournamentSortOrder
    {
        HIGHER_IS_BETTER,
        LOWER_IS_BETTER
    }

    public static class TournamentSortOrderExtensions
    {
        public static string ToValue(this TournamentSortOrder order)
        {
            return order switch
            {
                TournamentSortOrder.HIGHER_IS_BETTER => "HIGHER_IS_BETTER",
                TournamentSortOrder.LOWER_IS_BETTER => "LOWER_IS_BETTER",
                _ => order.ToString()
            };
        }
    }
}
