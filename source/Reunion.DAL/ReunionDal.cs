using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Reunion.Common;
using Reunion.Common.Model;
using Reunion.Common.Model.States;
using TUtils.Common;
using TUtils.Common.EF6;
using TUtils.Common.EF6.Transaction;

namespace Reunion.DAL
{
	/// <summary>
	/// implementation of IReunionDal
	/// </summary>
	public class ReunionDal<TDbContext> : IReunionDal
		where TDbContext : DbContext, IReunionDbContext
	{
		#region fields
		/// <summary>
		/// Maximum amount of old touch tasks in the database. (entity TouchTask)
		/// There is no need to store the whole history of touch requests in the database.
		/// </summary>
		private const int MaxCountTouchTasks = 100;

		private readonly ITransactionService<TDbContext> _transactionService;
		private readonly ISystemTimeProvider _systemTimeProvider;

		#endregion

		#region constructor

		/// <summary>
		/// 
		/// </summary>
		/// <param name="transactionService"></param>
		/// <param name="systemTimeProvider"></param>
		public ReunionDal(
			ITransactionService<TDbContext> transactionService,
			ISystemTimeProvider systemTimeProvider)
		{
			_transactionService = transactionService;
			_systemTimeProvider = systemTimeProvider;
		}

		#endregion

		#region private

		/// <summary>
		/// adds offline enumeration of TimeRange to given reunion in database
		/// </summary>
		/// <param name="timeRanges"></param>
		/// <param name="db"></param>
		/// <param name="player">
		/// must be attached in DbContext !
		/// </param>
		/// <param name="reunionEntity">
		/// must be attached in DbContext !
		/// </param>
		private static void AddDateRanges(
			IEnumerable<TimeRange> timeRanges, 
			TDbContext db,
			Player player, 
			ReunionEntity reunionEntity)
		{
			foreach (var d in timeRanges)
			{
				var dateRange = db.TimeRanges.Create();
				dateRange.Init(
					start: d.Start,
					end: d.End,
					preference: d.Preference,
					player: player,
					reunion: reunionEntity);
				db.TimeRanges.Add(dateRange);
			}
		}

		/// <summary>
		/// Creates online-entity Participant from offline entity "newParticipant"
		/// and inserts it into database.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="reunionEntity">
		/// must be attached in DbContext !
		/// </param>
		/// <param name="newParticipant">
		/// following members will be ignored and needn't to be filled:
		///		- Id
		///		- Reunion
		/// </param>
		/// <returns>
		/// created participant (attached to DbContext)
		/// </returns>
		private static Participant CreateParticipant(
			TDbContext db,
			ReunionEntity reunionEntity,
			Participant newParticipant)
		{
			var participantEntity = db.Participants.Create<Participant>().Init(
				id: 0,
				reunion: reunionEntity,
				isRequired: newParticipant.IsRequired,
				contactPolicy: newParticipant.ContactPolicy,
				name: newParticipant.Name,
				mailAddress: newParticipant.MailAddress,
				languageIsoCodeOfPlayer: newParticipant.LanguageIsoCodeOfPlayer,
				unguessableId: newParticipant.UnguessableId);
			db.Participants.Add(participantEntity);
			return participantEntity;
		}

		/// <summary>
		/// checks a new list of participants with an old list of participants
		/// and returns information about which participants are new, removed or 
		/// still in new list.
		/// </summary>
		/// <param name="newParticipants"></param>
		/// <param name="oldParticipants"></param>
		/// <param name="sameParticipants"></param>
		/// <param name="addedParticipants"></param>
		/// <param name="removedParticipants"></param>
		private static void GetChangesToListOfParticipants(
			IEnumerable<Participant> newParticipants,
			IEnumerable<Participant> oldParticipants,
			out List<Tuple<Participant, Participant>> sameParticipants,
			out List<Participant> addedParticipants,
			out List<Participant> removedParticipants)
		{
			sameParticipants = new List<Tuple<Participant, Participant>>();
			removedParticipants = new List<Participant>();

			foreach (var oldParticipant in oldParticipants)
			{
				bool found = false;
				// ReSharper disable once PossibleMultipleEnumeration
				foreach (var newParticipant in newParticipants)
				{
					if (newParticipant.Id == oldParticipant.Id)
					{
						found = true;
						sameParticipants.Add(new Tuple<Participant, Participant>(newParticipant, oldParticipant));
						break;
					}
				}

				if (!found)
				{
					removedParticipants.Add(oldParticipant);
				}
			}

			// ReSharper disable once PossibleMultipleEnumeration
			addedParticipants = newParticipants.Where(p => p.Id == 0).ToList();
		}

		/// <summary>
		/// returns true, if given reunion name allready exists in the set of reunions created by given user.
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="newReunionName"></param>
		/// <param name="db"></param>
		/// <param name="registeredUserId"></param>
		/// <returns></returns>
		private static bool ReunionNameAllreadyExist(int reunionId, string newReunionName, TDbContext db, string registeredUserId)
		{
			return
				(from organizer in db.Organizers
					join reunion in db.Reunions
						on organizer.Reunion.Id equals reunion.Id
					where
						organizer.UserId == registeredUserId
						&& reunion.Name == newReunionName
						&& reunionId != reunion.Id
					select reunion.Id)
					.Any();
		}

		/// <summary>
		/// shrinks table of TouchTasks untill it contains at maximum "MaxCountTouchTasks" (100) items.
		/// </summary>
		/// <param name="db"></param>
		private static void RemoveObsoleteTouchTasks(TDbContext db)
		{
			var count = db.TouchTasks.Count();
			if (count > MaxCountTouchTasks)
			{
				var removableTouchTasks = db.TouchTasks.OrderBy(t => t.Id).Take(count - MaxCountTouchTasks);
				db.TouchTasks.RemoveRange(removableTouchTasks);
			}
		}

		/// <summary>
		/// Deletes given player, it's statemachines and it's time ranges completely from database.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="player">
		/// must be attached to DbContext
		/// </param>
		private static void RemovePlayer(TDbContext db, Player player)
		{
			var statemachines = db.StateMachines.Where(s => s.PlayerId == player.Id).ToList();
			db.StateMachines.RemoveRange(statemachines);

			var timeRanges = db.TimeRanges.Where(r => r.Player.Id == player.Id).ToList();
			db.TimeRanges.RemoveRange(timeRanges);

			db.Players.Remove(player);
		}

		/// <summary>
		/// sets terminated status of all statemachines of given reunion
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="isTerminated"></param>
		private void SetTerminatedStatusOfStatemachines(int reunionId, bool isTerminated)
		{
			_transactionService.DoInTransaction(db =>
			{
				var allStatemachines = db.StateMachines.Where(s => s.ReunionId == reunionId).ToList();
				foreach (var statemachine in allStatemachines)
				{
					statemachine.IsTerminated = isTerminated;
				}
				db.SaveChanges();
			});
		}

		#endregion

		#region IReunionDal

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
		ReunionCreateResult IReunionDal.CreateReunion(
			ReunionEntity reunion,
			Organizer organizer,
			IEnumerable<Participant> participants,
			IEnumerable<TimeRange> possibleDateRanges)
		{
			return _transactionService.DoInTransaction(db =>
			{
				if (ReunionNameAllreadyExist(reunionId: 0,newReunionName: reunion.Name,db: db,registeredUserId: organizer.UserId))
					return new ReunionCreateResult(ReunionCreateResult.ResultCodeEnum.NameAllreadyExists, entity: null, error: null);

				// step 1: create reunion entity
				var reunionEntity = db.Reunions.Create().Init(
					reunionId: 0,
					name: reunion.Name,
					invitationText: reunion.InvitationText,
					deadline: reunion.Deadline,
					maxReactionTimeHours: reunion.MaxReactionTimeHours,
					deactivatedParticipants:null,
					finalInvitationDate:null);
				db.Reunions.Add(reunionEntity);
				db.SaveChanges();

				// step 2: create players 
				var organizerEnitity = db.Organizers.Create().Init(
					id: 0,
					userId: organizer.UserId,
					reunion: reunionEntity,
					languageIsoCodeOfPlayer: organizer.LanguageIsoCodeOfPlayer,
					name: organizer.Name,
					mailAddress: organizer.MailAddress);
				db.Organizers.Add(organizerEnitity);

				var createdParticipants = new List<Participant>();
				foreach (var p in participants)
					createdParticipants.Add(CreateParticipant(db, reunionEntity, p));
				db.Participants.AddRange(createdParticipants);

				db.SaveChanges();

				// step 3: create reunion statemachine, knock statemachine and DateRanges of organizer
				// Create statemachines of participants
				var organizerStatemachine = db.OrganizerStatemachines.Create().Init(
					reunionEntity,
					organizer: organizerEnitity,
					isTerminated: false,
					deadline: reunion.Deadline);
				db.OrganizerStatemachines.Add(organizerStatemachine);

				var knockStatemachine = db.StateMachines.Create<KnockStatemachineEntity>().Init(
					reunionEntity,
					organizer: organizerEnitity,
					isTerminated: false);
				db.StateMachines.Add(knockStatemachine);

				AddDateRanges(possibleDateRanges, db, organizerEnitity, reunionEntity);

				foreach (var p in createdParticipants)
				{
					var participantStatemachine = db.ParticipantStatemachines.Create().Init(reunionEntity, p, elapseDate: null, isTerminated: false);
					db.ParticipantStatemachines.Add(participantStatemachine);
				}

				db.SaveChanges();

				return new ReunionCreateResult(ReunionCreateResult.ResultCodeEnum.Succeeded, entity: reunionEntity, error: null);
			});
		}

		/// <summary>
		/// sets property StatemachineContext.IsTerminated=true of all statemachines of the given reunion
		/// </summary>
		/// <param name="reunionId"></param>
		void IReunionDal.DeactivateStatemachines(int reunionId)
		{
			SetTerminatedStatusOfStatemachines(reunionId, isTerminated: true);
		}

		/// <summary>
		/// deletes reunion
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="organizerId">
		/// organizer's id: neccessary for authorization 
		/// </param>
		void IReunionDal.DeleteReunion(int reunionId, int organizerId)
		{
			_transactionService.DoInTransaction(db =>
			{
				var reunion = (
					from r in db.Reunions
					join o in db.Organizers
						on r.Id equals o.Reunion.Id
					where r.Id == reunionId && o.Id == organizerId
					select r).FirstOrDefault();

				if (reunion == null)
					return;

				// remove statemachines and time ranges
				var timeRanges = db.TimeRanges.Where(s => s.Reunion.Id == reunionId).ToList();
				db.TimeRanges.RemoveRange(timeRanges);
				var statemachines = db.StateMachines.Where(s => s.Reunion.Id == reunionId).ToList();
				db.StateMachines.RemoveRange(statemachines);

				db.SaveChanges();

				// remove players
				var players = db.Players.Where(s => s.Reunion.Id == reunionId).ToList();
				db.Players.RemoveRange(players);

				db.SaveChanges();

				// remove reunion
				db.Reunions.Remove(reunion);
				db.SaveChanges();
			});
		}

		/// <summary>
		/// all transactions called in "action" will use the same DbContext object if possible.
		/// May be nested
		/// </summary>
		/// <param name="action">
		/// 
		/// </param>
		void IReunionDal.DoWithSameDbContext(Action action)
		{
			_transactionService.DoWithSameDbContext(action);
		}

		/// <summary>
		/// all transactions called in "action" will use the same DbContext object if possible.
		/// May be nested
		/// </summary>
		/// <param name="action">
		/// 
		/// </param>
		T IReunionDal.DoWithSameDbContext<T>(Func<T> action)
		{
			return _transactionService.DoWithSameDbContext(action);
		}


		/// <summary>
		/// returns the reunion id of the passed participant's unguessable id (Participant.UnguessableId)
		/// </summary>
		/// <param name="unguessableParticipantId"></param>
		/// <returns>id of reunion</returns>
		int IReunionDal.FindReunionIdOfParticipant(string unguessableParticipantId)
		{
			return _transactionService.DoInTransaction(db =>
			{
				return db.Participants
					.Where(p => p.UnguessableId == unguessableParticipantId)
					.Select(p => p.Reunion.Id)
					.FirstOrDefault();
			});
		}

		/// <summary>
		/// returns all active (StatemachineContext.IsTerminated==false) statemachines of all reunions
		/// </summary>
		/// <returns></returns>
		IEnumerable<StatemachineContext> IReunionDal.GetAllRunningStatemachines()
		{
			return _transactionService.DoInTransaction(db =>
			{
				return
					db.OrganizerStatemachines.Where(s => !s.IsTerminated).ToList().Cast<StatemachineContext>()
					.Concat(db.ParticipantStatemachines.Where(s => !s.IsTerminated).ToList())
					.Concat(db.KnockStatemachines.Where(s => !s.IsTerminated).ToList());
			});
		}

		/// <summary>
		/// returns all time ranges of the passed reunion
		/// </summary>
		/// <param name="reunionId"></param>
		/// <returns></returns>
		IEnumerable<TimeRange> IReunionDal.GetAllTimeRanges(int reunionId)
		{
			return _transactionService.DoInTransaction(db =>
			{
				return db.TimeRanges
					.Include(nameof(TimeRange.Player))
					.Where(t => t.Reunion.Id == reunionId)
					.ToList();
			});
		}

		/// <summary>
		/// Gets all reunions of a logged-in user
		/// </summary>
		/// <param name="userId">id of a registered user</param>
		/// <returns></returns>
		IEnumerable<ReunionEntity> IReunionDal.GetReunionsOfUser(string userId)
		{
			return _transactionService.DoInTransaction(db => (
				from r in db.Reunions
				join o in db.Organizers
					on r.Id equals o.Reunion.Id
				where o.UserId == userId
				select r).ToList());
		}

		/// <summary>
		/// gets time ranges of organizer
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="organizerId">
		/// for authorization only
		/// </param>
		/// <returns>!= null allways. If organizer doesn't exist, the call returns an empty enumeration</returns>
		IEnumerable<TimeRange> IReunionDal.GetTimeRangesOfOrganizer(int reunionId, int organizerId)
		{
			return _transactionService.DoInTransaction<IEnumerable<TimeRange>>(db =>
			{
				return db.TimeRanges.Where(t => t.Reunion.Id == reunionId && t.Player is Organizer && t.Player.Id == organizerId).ToList();
			});
		}

		/// <summary>
		/// gets time ranges of a given participant
		/// </summary>
		/// <param name="participantId"></param>
		/// <returns></returns>
		IEnumerable<TimeRange> IReunionDal.GetTimeRangesOfParticipant(int participantId)
		{
			return _transactionService.DoInTransaction<IEnumerable<TimeRange>>(db =>
			{
				return db.TimeRanges.Where(t => t.Player.Id == participantId).ToList();
			});
		}

		/// <summary>
		/// Loads 
		///		- reunion
		///		- organizer
		///		- participants
		///		- all statemachines
		/// </summary>
		/// <param name="reunionId"></param>
		/// <returns></returns>
		ReunionInfo IReunionDal.LoadReunion(int reunionId)
		{
			return _transactionService.DoInTransaction(db =>
			{
				var reunionInfo = (
					from r in db.Reunions
					join o in db.Organizers
						on r.Id equals o.Reunion.Id
					join p in db.Participants
						on r.Id equals p.Reunion.Id
					join os in db.OrganizerStatemachines
						on r.Id equals os.ReunionId
					join ks in db.KnockStatemachines
						on r.Id equals ks.ReunionId
					join ps in db.ParticipantStatemachines
						on p.Id equals ps.PlayerId
					where r.Id == reunionId
					group new {Participant=p,ParticipantStatemachine=ps} 
						by new { Reunion = r, Organizer = o, OrganizerStatemachine = os, KnockoutStatemachine = ks }
					into participantsOfReunion
					select participantsOfReunion)
					.ToList()
					.Select(x=> new ReunionInfo(
						x.Key.Reunion,
						x.Key.Organizer,
						x.Key.OrganizerStatemachine,
						x.Key.KnockoutStatemachine,
						x.Select(p => p.Participant).ToList(),
						x.Select(p => p.ParticipantStatemachine).ToList()
						))
					.FirstOrDefault();

				if (reunionInfo != null)
				{
					var reunion = reunionInfo.ReunionEntity;
					reunionInfo.Organizer.Reunion = reunion;
					int countParticipants = reunionInfo.Participants.Count();
					for (int i = 0; i < countParticipants; i++)
					{
						var player = reunionInfo.Participants[i];
						var statemachine = reunionInfo.ParticipantStatemachines[i];
						player.Reunion = reunion;
						statemachine.PlayerId = player.Id;
						statemachine.Reunion = reunion;
					}
					reunionInfo.KnockStatemachineEntity.Reunion = reunion;
					reunionInfo.KnockStatemachineEntity.PlayerId = reunionInfo.Organizer.Id;
				}
				return reunionInfo;
			});
		}

		/// <summary>
		/// Clears property StytemachineContext.IsTerminated = false of all statemachines of passed reunion
		/// </summary>
		/// <param name="reunionId"></param>
		void IReunionDal.ReactivateStatemachines(int reunionId)
		{
			SetTerminatedStatusOfStatemachines(reunionId, isTerminated: false);
		}

		/// <summary>
		/// Removes player from database and adds name of participant to string ReunionEntity.DeactivatedParticipants
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="participantId"></param>
		void IReunionDal.RemoveParticipant(int reunionId, int participantId)
		{
			_transactionService.DoInTransaction(db =>
			{
				var participant = db.Participants.Find(participantId);
				if ( participant == null )
					return;
				RemovePlayer(db, participant);
				var reunion = db.Reunions.Find(reunionId);
				if ( reunion == null )
					return;
				if (reunion.DeactivatedParticipants == null)
					reunion.DeactivatedParticipants = string.Empty;
				reunion.DeactivatedParticipants += $"{participant.Name} ";
				db.SaveChanges();
			});
		}

		/// <summary>
		/// set the given invitation date (ReunonEntity.FinalInvitationDate)
		/// </summary>
		/// <param name="reunionId"></param>
		/// <param name="finalInviationDate"></param>
		void IReunionDal.SaveFinalInvitationDate(int reunionId, DateTime? finalInviationDate)
		{
			_transactionService.DoInTransaction(db =>
			{
				var reunion = db.Reunions.Find(reunionId);
				if (reunion == null)
					return;
				reunion.FinalInvitationDate = finalInviationDate;
				db.SaveChanges();
			});
		}

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
		ReunionUpdateResult IReunionDal.UpdateReunion(
			ReunionEntity reunion,
			int verifiedOrganizerId,
			IEnumerable<Participant> participants,
			IEnumerable<TimeRange> possibleDateRanges)
		{
			return _transactionService.DoInTransaction(db =>
			{
				// load reunion including players and statemachines
				var reunionInfo = (this as IReunionDal).LoadReunion(reunion.Id);
				if (reunionInfo == null)
					return new ReunionUpdateResult(ReunionUpdateResult.ResultCodeEnum.ReunionNotExists, error: null);

				var reunionEntity = reunionInfo.ReunionEntity;
				var organizerEntity = reunionInfo.Organizer;

				// break if user isn't authorized
				if (organizerEntity.Id != verifiedOrganizerId)
					return new ReunionUpdateResult(ReunionUpdateResult.ResultCodeEnum.NotAuthorized, error: null);
				// if the new name of reunion would conflict with name of other existing reunion
				if (ReunionNameAllreadyExist(reunion.Id, reunion.Name, db, organizerEntity.UserId))
					return new ReunionUpdateResult(ReunionUpdateResult.ResultCodeEnum.NameAllreadyExists, error: null);

				// apply changes to reunion entity
				EntityExtension.ApplyChanges(reunion, reunionEntity, o=>o.DeactivatedParticipants, o =>o.FinalInvitationDate);

				// find changes to list of participants
				List<Participant> addedParticipants;
				List<Participant> removedParticipants;
				List<Tuple<Participant, Participant>> sameParticipants;

				GetChangesToListOfParticipants(
					newParticipants: participants,
					oldParticipants: reunionInfo.Participants,
					sameParticipants: out sameParticipants,
					addedParticipants: out addedParticipants,
					removedParticipants: out removedParticipants);

				// apply changes to participants
				foreach (var partcipant in sameParticipants)
					EntityExtension.ApplyChanges(partcipant.Item1, partcipant.Item2, ignoredProperties: p => p.UnguessableId);

				// remove deleted participants 
				foreach (var participant in removedParticipants)
					RemovePlayer(db, participant);

				db.SaveChanges();

				// create inserted participants
				var addedParticipantEntities = new List<Participant>();
				foreach (var participant in addedParticipants)
					addedParticipantEntities.Add(CreateParticipant(db, reunionEntity, participant));

				db.SaveChanges();

				// create participant statemachines for inserted participants
				foreach (var participantEntity in addedParticipantEntities)
				{
					var participantStatemachine = db.ParticipantStatemachines.Create().Init(
						reunionEntity, participantEntity, elapseDate: null, isTerminated: false);
					db.ParticipantStatemachines.Add(participantStatemachine);
				}

				// set new date ranges of organizer
				db.TimeRanges.RemoveRange(db.TimeRanges.Where(r => r.Player.Id == organizerEntity.Id).ToList());
				AddDateRanges(possibleDateRanges, db, organizerEntity, reunionEntity);

				db.SaveChanges();

				return new ReunionUpdateResult(ReunionUpdateResult.ResultCodeEnum.Succeeded, error: null);
			});
		}

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
		/// <param name="oldState">
		/// old state
		/// Precondition: current state must be equal old state, otherwise methof won't execute and will return false.
		/// </param>
		bool IReunionDal.UpdateState(StatemachineContext newState, int oldState)
		{
			if (!newState.IsDirty)
				return true;

			return _transactionService.DoInTransaction(db =>
			{
				var stateContextEntity =
					db.StateMachines.FirstOrDefault(
						s => s.Id == newState.Id);
				if (stateContextEntity == null)
					throw new ApplicationException("ehflkwke89324lah " + newState.StatemachineTypeId);

				if ( stateContextEntity.CurrentState != oldState)
					return false;

				EntityExtension.ApplyChanges(newState, stateContextEntity);

				db.SaveChanges();
				newState.IsDirty = false;
				return true;
			});
		}

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
		int IReunionDal.UpdateTimeRangesOfParticipant(IEnumerable<TimeRange> timeRanges, string unguessableParticipantId)
		{
			return _transactionService.DoInTransaction(db =>
			{
				var participant = db.Participants
					.Include(nameof(TimeRange.Reunion))
					.FirstOrDefault(p => p.UnguessableId == unguessableParticipantId);
				if (participant == null)
					return 0;
				var reunion = participant.Reunion;
				var participantId = participant.Id;
				var currentTimeRanges = db.TimeRanges.Where(t => t.Player.Id == participantId).ToList();
				db.TimeRanges.RemoveRange(currentTimeRanges);
				foreach (var t in timeRanges)
				{
					var newTimeRange = db.TimeRanges.Create();
					newTimeRange.Init(start: t.Start, end: t.End, preference: t.Preference, player: participant, reunion: reunion);
					db.TimeRanges.Add(newTimeRange);
				}
				db.SaveChanges();
				return reunion.Id;
			});
		}

		#region WebApi

		#region Touch

		/// <summary>
		/// returns touch task entity with the youngest creation time
		/// </summary>
		/// <returns></returns>
		TouchTask IReunionDal.GetLatestTouchTask()
		{
			return _transactionService.DoInTransaction(db =>
			{
				return db.TouchTasks.OrderByDescending(t => t.Id).FirstOrDefault();
			});
		}

		/// <summary>
		/// gets touch task with the given id
		/// </summary>
		/// <param name="touchTaskId"></param>
		/// <returns></returns>
		TouchTask IReunionDal.GetTouchTask(int touchTaskId)
		{
			return _transactionService.DoInTransaction(db => db.TouchTasks.Find(touchTaskId));
		}

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
		TouchTask IReunionDal.PutTouchTask(TouchTask touchTask)
		{
			return _transactionService.DoInTransaction(db =>
			{
				TouchTask touchTaskEntity;
				if (touchTask.Id != 0)
				{
					touchTaskEntity = db.TouchTasks.Find(touchTask.Id);
				}
				else
				{
					touchTaskEntity = db.TouchTasks.FirstOrDefault(t => t.CreationTimestamp == touchTask.CreationTimestamp);
				}

				if (touchTaskEntity == null)
				{
					touchTaskEntity = db.TouchTasks.Create();
					touchTaskEntity.CreationTimestamp = touchTask.CreationTimestamp;
					db.TouchTasks.Add(touchTaskEntity);
				}

				RemoveObsoleteTouchTasks(db);

				EntityExtension.ApplyChanges(touchTask, touchTaskEntity);

				db.SaveChanges();
				return touchTaskEntity;
			});
		}

		/// <summary>
		/// Creates and inserts a new touch task.
		/// Ensures that amount of (old) touch task resources don't increase more and more in database.
		/// Very old touch tasks will be deleted. 
		/// </summary>
		/// <returns>
		/// the created touch task
		/// </returns>
		public TouchTask PostTouchTask()
		{
			return _transactionService.DoInTransaction(db =>
			{
				var touchTaskEntity = db.TouchTasks.Create();
				touchTaskEntity.CreationTimestamp = _systemTimeProvider.LocalTime;

				db.TouchTasks.Add(touchTaskEntity);

				RemoveObsoleteTouchTasks(db);

				db.SaveChanges();
				return touchTaskEntity;
			});
		}

		/// <summary>
		/// Deletes the specified touch task. This method is idempotent.
		/// This method deletes the task-resource for the touch process only.
		/// It won't rollback any actions resulting from a former touch task.
		/// </summary>
		/// <param name="touchTaskId">
		/// id of the touch task
		/// </param>
		void IReunionDal.DeleteTouchTask(int touchTaskId)
		{
			_transactionService.DoInTransaction(db =>
			{
				var touchTask = db.TouchTasks.Find(touchTaskId);
				if (touchTask != null)
				{
					db.TouchTasks.Remove(touchTask);
					db.SaveChanges();
				}
			});
		}

		/// <summary>
		/// sets flag TouchTask.Executed of the specified touch task
		/// </summary>
		/// <param name="touchTaskId"></param>
		void IReunionDal.SetTouchTaskExecuted(int touchTaskId)
		{
			_transactionService.DoInTransaction(db =>
			{
				var touchTask = db.TouchTasks.Find(touchTaskId);
				if (touchTask != null)
				{
					if (!touchTask.Executed)
					{
						touchTask.Executed = true;
						db.SaveChanges();
					}
				}
			});
		}

		#endregion

		#endregion

		#endregion
	}
}
