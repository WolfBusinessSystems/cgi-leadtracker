using CGI.LeadTracker.Domain.AggregatesModel.Lead;

namespace CGI.LeadTracker.Domain.Services;

public static class FunnelEventMapper
{
    private static readonly IReadOnlyDictionary<FunnelStage, string> MetaEventNames =
        new Dictionary<FunnelStage, string>
        {
            { FunnelStage.LeadReceived,   "Lead"             },
            { FunnelStage.ProposalSent,   "InitiateCheckout" },
            { FunnelStage.ContractClosed, "Purchase"         }
        };

    private static readonly IReadOnlyDictionary<FunnelStage, string> GoogleConversionNames =
        new Dictionary<FunnelStage, string>
        {
            { FunnelStage.LeadReceived,   "lead_received"    },
            { FunnelStage.ProposalSent,   "proposal_sent"    },
            { FunnelStage.ContractClosed, "contract_closed"  }
        };

    public static bool ShouldSendEvent(FunnelStage stage) =>
        stage is FunnelStage.LeadReceived or FunnelStage.ProposalSent or FunnelStage.ContractClosed;

    public static string? GetMetaEventName(FunnelStage stage) =>
        MetaEventNames.TryGetValue(stage, out var name) ? name : null;

    public static string? GetGoogleConversionName(FunnelStage stage) =>
        GoogleConversionNames.TryGetValue(stage, out var name) ? name : null;
}
