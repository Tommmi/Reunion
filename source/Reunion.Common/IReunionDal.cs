using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reunion.Common.Model;
using Reunion.Common.Model.States;

namespace Reunion.Common
{
	/// <summary>
	/// data access lazer 
	/// </summary>
	public interface IReunionDal
	{
		/// <summary>
		/// Creates 
		///		- new reunion in database
		/// 	- organizer and participants.
		/// 	- statemachines of organizer.
		/// 	- possible date ranges.
		/// 	- statemachines of participants.
		/// </summary>
		/// <param name="reunion">
		/// Following members of reunion will be ignored and needn't to be filled:
		/// - Id
		/// - DeactivatedParticipants
		/// - FinalInvitationDate
		/// </param>
		/// <param name="organizer">
		/// Following members of Organizer will be ignored and needn't to be filled:
		/// - Id
		/// - Reunion
		/// </param>
		/// <param name="participants">
		/// Following members of Participant will be ignored and needn't to be filled:
		/// - Id
		/// - Reunion
		/// </param>
		/// <param name="possibleDateRanges">
		/// Date ranges of organizer.
		/// Following members needn't to be filled  and will be ignored:
		/// - Id
		/// - Player
		/// - Reunion
		/// </param>
		/// <returns></returns>
		ReunionCreateResult CreateReunion(
			ReunionEntity reunion,
			Organizer organizer,
			IEnumerable<Participant> participants,
			IEnumerable<TimeRange> possibleDateRanges);

		/// <summary>
		/// Removes player from database and adds name of participant to string ReunionEntity.DeactivatedParticipants
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="participantId"></param>
		void RemoveParticipant(int reunionId, int participantId);

		/// <summary>
		/// sets property StatemachineContext.IsTerminated=true of all statemachines of the given reunion
		/// </summary>
		/// <param name="reunionId"></param>
		void DeactivateStatemachines(int reunionId);

		/// <summary>
		/// deletes reunion
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="organizerId">
		/// organizer's id: neccessary for authorization 
		/// </param>
		void DeleteReunion(int reunionId, int organizerId);

		/// <summary>
		/// all transactions called in "action" will use the same DbContext object if possible.
		/// May be nested
		/// </summary>
		/// <param name="action">
		/// 
		/// </param>
		void DoWithSameDbContext(Action action);

		/// <summary>
		/// all transactions called in "action" will use the same DbContext object if possible.
		/// May be nested
		/// </summary>
		/// <param name="action">
		/// 
		/// </param>
		T DoWithSameDbContext<T>(Func<T> action);

		/// <summary>
		/// returns the reunion id of the passed participant's unguessable id (Participant.UnguessableId)
		/// </summary>
		/// <param name="unguessableParticipantId"></param>
		/// <returns>id of reunion</returns>
		int FindReunionIdOfParticipant(string unguessableParticipantId);

		/// <summary>
		/// returns all active (StatemachineContext.IsTerminated==false) statemachines of all reunions
		/// </summary>
		/// <returns></returns>
		IEnumerable<StatemachineContext> GetAllRunningStatemachines();

		/// <summary>
		/// returns all time ranges of the passed reunion
		/// </summary>
		/// <param name="reunionId"></param>
		/// <returns></returns>
		IEnumerable<TimeRange> GetAllTimeRanges(int reunionId);

		/// <summary>
		/// gets time ranges of organizer
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="organizerId">
		/// for authorization only
		/// </param>
		/// <returns>!= null allways. If organizer doesn't exist, the call returns an empty enumeration</returns>
		IEnumerable<TimeRange> GetTimeRangesOfOrganizer(int reunionId, int organizerId);

		/// <summary>
		/// Gets all reunions of a logged-in user
		/// </summary>
		/// <param name="userId">id of a registered user</param>
		/// <returns></returns>
		IEnumerable<ReunionEntity> GetReunionsOfUser(string userId);

		/// <summary>
		/// gets time ranges of a given participant
		/// </summary>
		/// <param name="participantId"></param>
		/// <returns></returns>
		IEnumerable<TimeRange> GetTimeRangesOfParticipant(int participantId);

		/// <summary>
		/// Loads 
		///		- reunion
		///		- organizer
		///		- participants
		///		- all statemachines
		/// </summary>
		/// <param name="reunionId"></param>
		/// <returns></returns>
		ReunionInfo LoadReunion(int reunionId);

		/// <summary>
		/// Clears property StytemachineContext.IsTerminated = false of all statemachines of passed reunion
		/// </summary>
		/// <param name="reunionId"></param>
		void ReactivateStatemachines(int reunionId);

		/// <summary>
		/// set the given invitation date (ReunonEntity.FinalInvitationDate)
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="finalInviationDate"></param>
		void SaveFinalInvitationDate(int reunionId, DateTime? finalInviationDate);

		/// <summary>
		/// Updates reunion.
		/// </summary>
		/// <param name="reunion">
		/// Following members needn't to be filled and will be ignored:
		/// - DeactivatedParticipants
		/// - FinalInvitationDate
		/// </param>
		/// <param name="verifiedOrganizerId">
		/// the id of the organizer
		/// </param>
		/// <param name="participants">
		/// Following members needn't to be filled and will be ignored:
		/// - Id: must be 0 if it's new !
		/// - Reunion
		/// - UnguessableId, if Id != 0
		/// </param>
		/// <param name="possibleDateRanges">
		/// Date ranges of organizer.
		/// Following members needn't to be filled  and will be ignored:
		/// - Player
		/// - Reunion
		/// - Id:  must be 0, if it's new !
		/// </param>
		/// <returns></returns>
		ReunionUpdateResult UpdateReunion(
			ReunionEntity reunion,
			int verifiedOrganizerId,
			IEnumerable<Participant> participants,
			IEnumerable<TimeRange> possibleDateRanges);

		/// <summary>
		/// Saves changes to entity StatemachineContext.
		/// If newState.IsDirty == false, the method doese nothing.
		/// Saves all of the entity except
		/// - Id
		/// - IsDirty
		/// - Player
		/// - Reunion
		/// </summary>
		/// <param name="newState">
		/// Following members needn't to be filled  and will be ignored:
		/// - Player
		/// - Reunion
		/// </param>
		void UpdateState(StatemachineContext newState);

		///  <summary>
		///  Replaces time ranges of participant
		///  </summary>
		///  <param name="timeRanges">
		///  following members of TimeRange will be ignored and you needn't fill in:
		/// 		TimeRange.Id
		/// 		TimeRange.Player
		/// 		TimeRange.Reunion
		///  </param>
		///  <param name="unguessableParticipantId"></param>
		/// <returns>reunion id</returns>
		int UpdateTimeRangesOfParticipant(IEnumerable<TimeRange> timeRanges, string unguessableParticipantId);

		#region api

		#region Touch
		/// <summary>
		/// returns touch task entity with the youngest creation time
		/// </summary>
		/// <returns></returns>
		TouchTask GetLatestTouchTask();

		/// <summary>
		/// gets touch task with the given id
		/// </summary>
		/// <param name="touchTaskId"></param>
		/// <returns></returns>
		TouchTask GetTouchTask(int touchTaskId);

		/// <summary>
		/// Upserts touch task. This method is idempotent.
		/// If touch task was found either by TouchTask.Id or TouchTask.CreationTimestamp,
		/// the existing entity will be updated.
		/// Ensures that amount of (old) touch task resources don't increase more and more in database.
		/// Very old touch tasks will be deleted. 
		/// </summary>
		/// <param name="touchTask">
		/// if touchTask.Id==0, the method will look by touchTask.CreationTimestamp for the existing entity.
		/// </param>
		/// <returns></returns>
		TouchTask PutTouchTask(TouchTask touchTask);

		/// <summary>
		/// Creates and inserts a new touch task.
		/// Ensures that amount of (old) touch task resources don't increase more and more in database.
		/// Very old touch tasks will be deleted. 
		/// </summary>
		/// <returns>
		/// the created touch task
		/// </returns>
		TouchTask PostTouchTask();

		/// <summary>
		/// Deletes the specified touch task. This method is idempotent.
		/// This method deletes the task-resource for the touch process only.
		/// It won't rollback any actions resulting from a former touch task.
		/// </summary>
		/// <param name="touchTaskId">
		/// id of the touch task
		/// </param>
		void DeleteTouchTask(int touchTaskId);

		/// <summary>
		/// sets flag TouchTask.Executed of the specified touch task
		/// </summary>
		/// <param name="touchTaskId"></param>
		void SetTouchTaskExecuted(int touchTaskId);

		#endregion

		#endregion
	}

	/// <summary>
	/// result info of method CreateReunion()
	/// </summary>
	public class ReunionCreateResult
	{
		public enum ResultCodeEnum
		{
			Succeeded,
			NameAllreadyExists
		}

		public ResultCodeEnum ResultCode { get; private set; }
		/// <summary>
		/// == null, if ResultCode != ResultCodeEnum.Succeeded
		/// </summary>
		public ReunionEntity Entity { get; private set; }

		/// <summary>
		/// != null, if any 
		/// </summary>
		public Exception Error { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="resultCode"></param>
		/// <param name="entity"></param>
		/// <param name="error"></param>
		public ReunionCreateResult(ResultCodeEnum resultCode, ReunionEntity entity, Exception error)
		{
			ResultCode = resultCode;
			Entity = entity;
			Error = error;
		}
	}

	/// <summary>
	/// result type of DAL-method UpdateReunion()
	/// </summary>
	public class ReunionUpdateResult
	{
		public enum ResultCodeEnum
		{
			Succeeded,
			ReunionNotExists,
			NotAuthorized,
			NameAllreadyExists
		}
		public ResultCodeEnum ResultCode { get; private set; }
		public Exception Error { get; private set; }

		public ReunionUpdateResult(ResultCodeEnum resultCode, Exception error)
		{
			ResultCode = resultCode;
			Error = error;
		}
	}
}
