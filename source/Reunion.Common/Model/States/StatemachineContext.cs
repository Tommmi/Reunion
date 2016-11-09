using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reunion.Common.Model.States
{
	/// <summary>
	/// Base class entity for the current state of a statemachine in Reunion.
	/// </summary>
	public class StatemachineContext
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="currentState"></param>
		/// <param name="player">
		/// The meaning of the player, which is associated to a Reunion statemachine 
		/// depends on the concrete statemachine. 
		/// </param>
		/// <param name="reunion">
		/// The reunion to which the statemachine is associated to. 
		/// A statemachine in Reunion is allways associated to exact one reunion.
		/// </param>
		/// <param name="elapseDate">
		/// meaning depends on the concrete statemachine.
		/// </param>
		/// <param name="isTerminated">
		/// A statemachine can be terminated, which means that elapseDate needn't to be checked any more.
		/// </param>
		/// <param name="statemachineTypeId">
		/// type id of the statemachine
		/// </param>
		public StatemachineContext(
			int currentState, 
			Player player,
			ReunionEntity reunion,
			DateTime? elapseDate,
			bool isTerminated,
			StatemachineIdEnum statemachineTypeId)
		{
			Init(
				currentState,
				player,
				reunion,
				elapseDate??DateTime.Now,
				isTerminated,
				statemachineTypeId);
		}

		public StatemachineContext()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="currentState"></param>
		/// <param name="player">
		/// The meaning of the player, which is associated to a Reunion statemachine 
		/// depends on the concrete statemachine. 
		/// </param>
		/// <param name="reunion">
		/// The reunion to which the statemachine is associated to. 
		/// A statemachine in Reunion is allways associated to exact one reunion.
		/// </param>
		/// <param name="elapseDate">
		/// meaning depends on the concrete statemachine.
		/// </param>
		/// <param name="isTerminated">
		/// A statemachine can be terminated, which means that elapseDate needn't to be checked any more.
		/// </param>
		/// <param name="statemachineTypeId">
		/// type id of the statemachine
		/// </param>
		protected void Init(
			int currentState,
			Player player,
			ReunionEntity reunion,
			DateTime? elapseDate,
			bool isTerminated,
			StatemachineIdEnum statemachineTypeId
			)
		{
			CurrentState = currentState;
			PlayerId = player?.Id??0;
			//PlayerId = player.Id;
			Reunion = reunion;
			ReunionId = reunion.Id;
			ElapseDate = elapseDate??DateTime.Now;
			StatemachineTypeId = statemachineTypeId;
			IsTerminated = isTerminated;
		}

		/// <summary>
		/// Hint ! Will be set automatically by methods State.SetNewState() and ReunionDal.UpdateState()
		/// </summary>
		[NotMapped]
		public bool IsDirty { get; set; }

		public int Id { get; set; }
		public int CurrentState {  get; set; }
		/// <summary>
		/// The meaning of the player, which is associated to a Reunion statemachine 
		/// depends on the concrete statemachine. 
		/// </summary>
		public int PlayerId { get; set; }
		///// <summary>
		///// The meaning of the player, which is associated to a Reunion statemachine 
		///// depends on the concrete statemachine. 
		///// </summary>
		//public Player Player { get; set; }
		/// <summary>
		/// The reunion to which the statemachine is associated to. 
		/// A statemachine in Reunion is allways associated to exact one reunion.
		/// </summary>
		public ReunionEntity Reunion { get; set; }
		/// <summary>
		/// The reunion to which the statemachine is associated to. 
		/// A statemachine in Reunion is allways associated to exact one reunion.
		/// </summary>
		public int ReunionId {  get; set; }
		/// <summary>
		/// type id of the statemachine
		/// </summary>
		public StatemachineIdEnum StatemachineTypeId { get; set; }
		/// <summary>
		/// meaning depends on the concrete statemachine.
		/// </summary>
		public DateTime ElapseDate { get; set; }
		/// <summary>
		/// A statemachine can be terminated, which means that elapseDate needn't to be checked any more.
		/// </summary>
		public bool IsTerminated { get; set; }
	}
}
