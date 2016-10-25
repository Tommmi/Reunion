using System.Data.Entity;
using Reunion.Common.Model;
using Reunion.Common.Model.States;

namespace Reunion.DAL
{
	public class ReunionDbContext : DbContext
	{
		public ReunionDbContext() : base("ReunionConnectionString")
		{
			Database.SetInitializer(new MigrateDatabaseToLatestVersion<ReunionDbContext, ReunionDbConfiguration>("ReunionConnectionString"));
		}

		public virtual DbSet<Player> Players { get; set; }
		public virtual DbSet<Organizer> Organizers { get; set; }
		public virtual DbSet<Participant> Participants { get; set; }
		public virtual DbSet<ReunionEntity> Reunions { get; set; }
		public virtual DbSet<TimeRange> TimeRanges { get; set; }
		public virtual DbSet<StatemachineContext> StateMachines { get; set; }
		public virtual DbSet<OrganizerStatemachineEntity> OrganizerStatemachines { get; set; }
		public virtual DbSet<ParticipantStatemachineEntity> ParticipantStatemachines { get; set; }
		public virtual DbSet<KnockStatemachineEntity> KnockStatemachines { get; set; }
		public virtual DbSet<TouchTask> TouchTasks { get; set; }
	}
}
