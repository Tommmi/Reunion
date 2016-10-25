namespace Reunion.Common.Model.States
{
	public enum OrganizatorStatusEnum
	{
		/// <summary>
		/// Default
		/// </summary>
		Created = 0,
		/// <summary>
		/// reunion is looking for an apropiate day
		/// </summary>
		Running = 1,
		/// <summary>
		/// reunion has found at minimum one day
		/// </summary>
		DateProposal = 2,
		/// <summary>
		/// reunion has sent final invitations to all participants
		/// </summary>
		FinalInvitation = 3,
		/// <summary>
		/// participants accepted final date proposal
		/// </summary>
		FinishedSucceeded = 4,
		/// <summary>
		/// deadline for plannning reached
		/// </summary>
		FinishedFailed = 5,
	}
}
