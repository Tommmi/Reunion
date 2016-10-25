using System;
using System.ComponentModel.DataAnnotations;

namespace Reunion.Common.Model
{
	public class TaskBase
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public ReunionEntity Reunion { get; set; }
		[Required]
		public TaskTypeEnum TaskType { get; set; }
		[Required]
		public DateTime StartTime { get; set; }
		[Required]
		public DateTime Deadline { get; set; }
		[Required]
		public Player Player { get; set; }
		[Required]
		public bool Done { get; set; }

		public TaskBase()
		{

		}

		public TaskBase(
			ReunionEntity reunion,
			TaskTypeEnum taskType,
			DateTime startTime,
			DateTime deadline,
			Player player,
			bool done)
		{
			Reunion = reunion;
			TaskType = taskType;
			StartTime = startTime;
			Deadline = deadline;
			Player = player;
			Done = done;
		}
	}
}