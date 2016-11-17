using System.Data.Entity;
using Reunion.Common.Model;
using Reunion.Common.Model.States;

namespace Reunion.DAL
{
	/// <summary>
	/// entity framework database context - productive version
	/// </summary>
	public class ReunionDbContext : DbContext, IReunionDbContext
	{
		public ReunionDbContext() : base("ReunionConnectionString")
		{
			// migrate automatically
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
