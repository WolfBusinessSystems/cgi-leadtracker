namespace CGI.LeadTracker.Domain.AggregatesModel.Lead;

public enum FunnelStage
{
    LeadReceived   = 1,
    ContactMade    = 2,
    ProposalSent   = 3,
    ContractClosed = 4,
    Disqualified   = 99  // terminal — pode ocorrer em qualquer etapa
}
