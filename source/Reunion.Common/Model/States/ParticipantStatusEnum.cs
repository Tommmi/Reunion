namespace Reunion.Common.Model.States
{
	public enum ParticipantStatusEnum
	{
		/// <summary>
		/// default
		/// </summary>
		Created = 0,
		/// <summary>
		/// waiting on visitation of participant after getting the first invitation
		/// </summary>
		WaitOnLoginForInvitation = 1,
		/// <summary>
		/// participant has got first invitation and has visited the site at minimum one time
		/// </summary>
		Invitated = 2,
		/// <summary>
		/// participant hasn't visited the site after getting the first invitation for a long time
		/// </summary>
		ReactionOnInvitationMissing = 3,
		/// <summary>
		/// participant has got the final invitation with a concrete day
		/// </summary>
		FinallyInvitated = 4,
		/// <summary>
		/// participant hasn't answered to the final invitation yet for a long time
		/// </summary>
		ReactionOnFinalInvitationMissing = 6,
		/// <summary>
		/// participant rejected the final invitation
		/// </summary>
		RejectedInvitation = 7,
		/// <summary>
		/// participant accepted the final invitation
		/// </summary>
		Accepted = 8,
	}
}