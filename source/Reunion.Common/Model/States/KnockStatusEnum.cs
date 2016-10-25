namespace Reunion.Common.Model.States
{
	public enum KnockStatusEnum
	{
		/// <summary>
		/// Default
		/// </summary>
		Idle = 0,
		/// <summary>
		/// organizer has been notified recently, but organizer hasn't logged in since that time any more
		/// </summary>
		WaitForLogin = 1,
		/// <summary>
		/// 
		/// </summary>
		Stopped = 2
	}
}