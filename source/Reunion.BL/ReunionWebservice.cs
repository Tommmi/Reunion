using System;
using Reunion.Common;
using Reunion.Common.Model;
using TUtils.Common;

namespace Reunion.BL
{
	/// <summary>
	/// implements IReunionWebservice
	/// all REST functions, grouped by resources
	/// </summary>
	public class ReunionWebservice : IReunionWebservice
	{
		/// <summary>
		/// REST function Touch
		/// </summary>
		private class Touch : ITouch
		{
			private readonly IReunionDal _dal;
			private readonly IReunionBL _bl;
			private readonly IDebouncer _debouncer;

			public Touch(IReunionDal dal, IReunionBL bl, IDebouncer debouncer)
			{
				_dal = dal;
				_bl = bl;
				_debouncer = debouncer;
			}

			TouchTask ITouch.Get()
			{
				return _dal.GetLatestTouchTask();
			}

			TouchTask ITouch.Get(int id)
			{
				return _dal.GetTouchTask(id);
			}

			int ITouch.Put(DateTime creationTimestamp)
			{
				var createdTouchTask = _dal.PutTouchTask(new TouchTask(id: 0, creationTimestamp: creationTimestamp));
				if (!createdTouchTask.Executed)
				{
					if ( !_debouncer.ShouldIgnore(this))
						_bl.TouchAllReunions();
					_dal.SetTouchTaskExecuted(createdTouchTask.Id);
				}
				return createdTouchTask.Id;
			}

			int ITouch.Post()
			{
				var createdTouchTask = _dal.PostTouchTask();
				if (!_debouncer.ShouldIgnore(this))
					_bl.TouchAllReunions();
				_dal.SetTouchTaskExecuted(createdTouchTask.Id);
				return createdTouchTask.Id;
			}

			void ITouch.Delete(int id)
			{
				_dal.DeleteTouchTask(id);
			}
		}


		private readonly ITouch _touch;

		public ReunionWebservice(IReunionDal dal, IReunionBL bl, IDebouncer debouncer)
		{
			_touch = new Touch(dal, bl, debouncer);
		}

		ITouch IReunionWebservice.Touch => _touch;
	}
}