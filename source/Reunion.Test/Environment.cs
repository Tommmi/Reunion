using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using NSubstitute;
using Reunion.BL;
using Reunion.Common;
using Reunion.Common.Model;
using Reunion.Common.Model.States;
using Reunion.DAL;
using Reunion.Web.Common;
using Reunion.Web.Controllers;
using TUtils.Common;
using TUtils.Common.EF6.Transaction.Common;
using TUtils.Common.Logging;
using TUtils.Common.Logging.Common;
using TUtils.Common.Logging.LogMocs;

namespace Reunion.Test
{
	public class Environment
	{
		private readonly int _minimumWaitTimeSeconds;

		#region types

		private class BlResourceMoc : IBlResource
		{
			private string GetString(string name, int countParameters, CultureInfo cultureInfo =null)
			{
				var res = $"name={name}";
				for (var i = 0; i < countParameters; i++)
				{
					res += $",{i}={{{i}}}";
				}
				if (cultureInfo != null)
					res += $",cultureInfo={cultureInfo.TwoLetterISOLanguageName}";
				return res;
			}
		

			/// <summary>
			/// subject of mail which invites participant to reunion
			/// {0}: Reunion.Name
			/// </summary>
			string IBlResource.GetInvitationMailSubject(CultureInfo cultureInfo)
			{
				return GetString(nameof(IBlResource.GetInvitationMailSubject),1,cultureInfo);
			}

			/// <summary>
			/// body of mail for organizer to invite all participants
			/// {0}: Reunion.Name
			/// {1}: Reunion.InvitationText,
			/// {2}: Reunion.Deadline,
			/// {3}: mail address of web service
			/// </summary>
			string IBlResource.InvitationMailBody => GetString(nameof(IBlResource.InvitationMailBody),4);

			/// <summary>
			/// body of mail which contains the individual link to the site
			/// {0}: Reunion.Name
			/// {1}: Reunion.InvitationText,
			/// {2}: Reunion.Deadline,
			/// {3}: direct link to web site
			/// </summary>
			string IBlResource.GetInvitationMailBodyParticipant(CultureInfo cultureInfo)
			{
				return GetString(nameof(IBlResource.GetInvitationMailBodyParticipant), 4, cultureInfo);
			}

			/// <summary>
			/// subject of mail which awakes organizer
			/// {0}: Reunion.Name
			/// </summary>
			string IBlResource.GetKnockMailSubject(CultureInfo cultureInfo)
			{
				return GetString(nameof(IBlResource.GetKnockMailSubject), 1, cultureInfo);
			}

			/// <summary>
			/// body of mail which awakes organizer
			/// {0}: Reunion.Name
			/// {1}: link to organizers status page
			/// </summary>
			string IBlResource.GetKnockMailBody(CultureInfo cultureInfo)
			{
				return GetString(nameof(IBlResource.GetKnockMailBody), 2, cultureInfo);
			}

			/// <summary>
			/// subject of mail which finally invites participant to reunion
			/// {0}: Reunion.Name
			/// </summary>
			string IBlResource.GetFinalInvitationMailSubject(CultureInfo cultureInfo)
			{
				return GetString(nameof(IBlResource.GetFinalInvitationMailSubject), 1, cultureInfo);
			}

			/// <summary>
			/// body of mail which contains the individual link to the site
			/// {0}: Reunion.Name
			/// {1}: date of reunion,
			/// {2}: direct link to web site
			/// </summary>
			string IBlResource.GetFinalInvitationMailBodyParticipant(CultureInfo cultureInfo)
			{
				return GetString(nameof(IBlResource.GetFinalInvitationMailBodyParticipant), 3, cultureInfo);
			}

			/// <summary>
			/// body of mail which informs organizer about final invitation
			/// {0}: Reunion.Name
			/// {1}: date of reunion,
			/// </summary>
			string IBlResource.FinalInvitationMailBody => GetString(nameof(IBlResource.FinalInvitationMailBody), 2);


			/// <summary>
			/// subject of mail which rejects final invitation to reunion
			/// {0}: Reunion.Name
			/// </summary>
			string IBlResource.GetRejectionMailSubject(CultureInfo cultureInfo)
			{
				return GetString(nameof(IBlResource.GetRejectionMailSubject), 1, cultureInfo);
			}

			/// <summary>
			/// body of mail which contains the individual link to the site and 
			/// the rejection of the final invitation
			/// {0}: Reunion.Name
			/// {1}: date of reunion,
			/// {2}: direct link to web site
			/// </summary>
			string IBlResource.GetRejectionMailBodyParticipant(CultureInfo cultureInfo)
			{
				return GetString(nameof(IBlResource.GetRejectionMailBodyParticipant), 3, cultureInfo);
			}

			/// <summary>
			/// subject of mail which informs participant about that he hasn't filled in some important days 
			/// in the calendar.
			/// {0}:reunion name
			/// </summary>
			/// <param name="cultureInfo"></param>
			/// <returns></returns>
			string IBlResource.GetMissingDayNotificationMailSubject(CultureInfo cultureInfo)
			{
				return GetString(nameof(IBlResource.GetMissingDayNotificationMailSubject), 1, cultureInfo);
			}

			/// <summary>
			/// body of mail which informs participant about that he hasn't filled in some important days 
			/// in the calendar.
			/// {0}:reunion name
			/// {1}:missing days: e.g.: "01.10.2016, 02.10.2016"
			/// {2}:direct link
			/// </summary>
			/// <param name="cultureInfo"></param>
			/// <returns></returns>
			string IBlResource.GetMissingDayNotificationBody(CultureInfo cultureInfo)
			{
				return GetString(nameof(IBlResource.GetMissingDayNotificationBody), 3, cultureInfo);
			}
		}

		private class ReunionDbTestContext : DbContextBase<ReunionDbTestContext>, IReunionDbContext
		{
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

		#endregion

		private IIdentityManager _identityManager;
		private IAppSettings _appSettings;
		private TransactionService<ReunionDbTestContext> _transactionService;
		private BlResourceMoc _resource;

		#region public properties

		public ReunionController ReunionController { get; private set; }
		public ParticipantController ParticipantController { get; private set; }
		public IReunionBL Bl { get; private set; }
		public IReunionDal Dal { get; private set; }
		public EmailSenderMoc EmailSender { get; private set; }
		public IReunionUser CurrentUser { get; private set; }
		public SystemTimeProviderMoc SystemTimeProvider { get; private set; }
		#endregion

		#region constructor

		public Environment(
			string testMethodName,
			int minimumWaitTimeSeconds,
			bool isUserAuthenticated,
			DateTime startTime)
		{
			_minimumWaitTimeSeconds = minimumWaitTimeSeconds;
			var logWriter = new LogConsoleWriter(
				minSeverity: LogSeverityEnum.WARNING,
				namespacesWhiteList: new List<string> {"*"},
				namespacesBlackList: new List<string> { });
			var logger = new TLog(
				logWriter,
				isLoggingOfMethodNameActivated:false);

			var dbContextFactory = new DbContextFactory4Unittest<ReunionDbTestContext>(testMethodName);
			_transactionService = new TransactionService<ReunionDbTestContext>(
				logger,
				dbContextFactory,
				IsolationLevel.Serializable);

			SystemTimeProvider = new SystemTimeProviderMoc(startTime);

			Dal = new ReunionDal<ReunionDbTestContext>(_transactionService, SystemTimeProvider);

			EmailSender = new EmailSenderMoc();

			_resource = new BlResourceMoc();

			_appSettings = Substitute.For<IAppSettings>();
			_appSettings.MailAccount_MailAddress.Returns("findtime@gmx.de");
			_appSettings.MailAccount_Password.Returns("password");
			_appSettings.MailAccount_SmtpHost.Returns("smtphost");
			_appSettings.MailAccount_UserName.Returns("username");
			_appSettings.ServiceHost.Returns("localhost");
			_appSettings.StartPage4Participant.Returns("StartPage4Participant/{0}");
			_appSettings.StatusPageOfReunion.Returns("StatusPageOfReunion/{0}");


			var currentUser = Substitute.For<IReunionUser>();
			currentUser.GetEmail().Returns("currentuser@mail");
			currentUser.GetId().Returns("89798174");
			currentUser.GetName().Returns("current user");
			CurrentUser = currentUser;

			_identityManager = Substitute.For<IIdentityManager>();
			_identityManager.GetCurrentUser().Returns(isUserAuthenticated?currentUser:null);
			_identityManager.IsUserAuthenticated.Returns(isUserAuthenticated);
			_identityManager.GetLanguageOfCurrentUser().Returns("de");
			_identityManager.GetVerifiedOrganizer(Arg.Any<int>()).Returns(callInfo =>
			{
				var cu = _identityManager.GetCurrentUser();
				if (cu == null)
					return null;
				return Bl.GetOrganizer(reunionId:callInfo.Arg<int>(), userId: cu.GetId());
			});

			ClearRequestCache();
		}

		#endregion

		public void ClearRequestCache()
		{
			Bl = new ReunionBL(
				SystemTimeProvider,
				_transactionService,
				Dal,
				EmailSender,
				_resource,
				minimumWaitTimeSeconds: _minimumWaitTimeSeconds,
				startPage4Participant: _appSettings.StartPage4Participant,
				statusPageOfReunion: _appSettings.StatusPageOfReunion,
				mailAddressOfReunion: _appSettings.MailAccount_MailAddress);


			ReunionController = new ReunionController(
				Bl,
				_identityManager,
				_appSettings);

			ParticipantController = new ParticipantController(
				Bl,
				_identityManager);
		}
	}
}
