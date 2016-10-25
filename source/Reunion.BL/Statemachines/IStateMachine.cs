namespace Reunion.BL.Statemachines
{
	/// <summary>
	/// non-generic common interface of all statemachines
	/// </summary>
	public interface IStateMachine
	{
		/// <summary>
		/// This method must be called time by time to ensure that actions take place when some time markers elapsed.
		/// It is implemented by statemachines individually to check (by polling) time dependent events.
		/// </summary>
		void Touch();

		/// <summary>
		/// id of reunion. Note ! a statemachine in Reunion allways belongs exactly to one reunion entity
		/// </summary>
		int ReunionId { get; }
	}
}