namespace Reunion.Common.Model
{
	public class SystemPlayer : Player
	{
		public SystemPlayer()
		{

		}

		public SystemPlayer(ReunionEntity reunion) : base(userId:"system", reunion:reunion)
		{

		}
	}

}