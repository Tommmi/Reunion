using System.Data.Entity;
using Reunion.Common.Model;
using Reunion.Common.Model.States;

namespace Reunion.DAL
{
	public interface IReunionDbContext
	{
		DbSet<KnockStatemachineEntity> KnockStatemachines { get; set; }
		DbSet<Organizer> Organizers { get; set; }
		DbSet<OrganizerStatemachineEntity> OrganizerStatemachines { get; set; }
		DbSet<Participant> Participants { get; set; }
		DbSet<ParticipantStatemachineEntity> ParticipantStatemachines { get; set; }
		DbSet<Player> Players { get; set; }
		DbSet<ReunionEntity> Reunions { get; set; }
		DbSet<StatemachineContext> StateMachines { get; set; }
		DbSet<TimeRange> TimeRanges { get; set; }
		DbSet<TouchTask> TouchTasks { get; set; }
	}
}