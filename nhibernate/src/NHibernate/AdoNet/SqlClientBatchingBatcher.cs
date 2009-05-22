using System.Data;
using System.Text;
using NHibernate.AdoNet.Util;

namespace NHibernate.AdoNet
{
	/// <summary>
	/// Summary description for SqlClientBatchingBatcher.
	/// </summary>
	internal class SqlClientBatchingBatcher : AbstractBatcher
	{
		private int batchSize;
		private int totalExpectedRowsAffected;
		private SqlClientSqlCommandSet currentBatch;
		private StringBuilder currentBatchCommandsLog;

		public SqlClientBatchingBatcher(ConnectionManager connectionManager, IInterceptor interceptor)
			: base(connectionManager, interceptor)
		{
			batchSize = Factory.Settings.AdoBatchSize;
			currentBatch = new SqlClientSqlCommandSet();
			//we always create this, because we need to deal with a scenario in which
			//the user change the logging configuration at runtime. Trying to put this
			//behind an if(log.IsDebugEnabled) will cause a null reference exception 
			//at that point.
			currentBatchCommandsLog = new StringBuilder();
		}

		public override int BatchSize
		{
			get { return batchSize; }
			set { batchSize = value; }
		}

		public override void AddToBatch(IExpectation expectation)
		{
			totalExpectedRowsAffected += expectation.ExpectedRowCount;
			IDbCommand batchUpdate = CurrentCommand;

			string lineWithParameters = null;
			if (Factory.Settings.SqlStatementLogger.IsDebugEnabled)
			{
				lineWithParameters = Factory.Settings.SqlStatementLogger.GetCommandLineWithParameters(batchUpdate);
				currentBatchCommandsLog.Append("Batch command: ").AppendLine(lineWithParameters);
			}
			if (log.IsDebugEnabled)
			{
				log.Debug("Adding to batch:" + lineWithParameters);
			}
			currentBatch.Append((System.Data.SqlClient.SqlCommand) batchUpdate);

			if (currentBatch.CountOfCommands >= batchSize)
			{
				DoExecuteBatch(batchUpdate);
			}
		}

		protected override void DoExecuteBatch(IDbCommand ps)
		{
			log.Debug("Executing batch");
			CheckReaders();
			Prepare(currentBatch.BatchCommand);
			if (Factory.Settings.SqlStatementLogger.IsDebugEnabled)
			{
				Factory.Settings.SqlStatementLogger.LogBatchCommand(currentBatchCommandsLog.ToString());
				currentBatchCommandsLog = new StringBuilder();
			}
			
			int rowsAffected = currentBatch.ExecuteNonQuery();

			Expectations.VerifyOutcomeBatched(totalExpectedRowsAffected, rowsAffected);

			currentBatch.Dispose();
			totalExpectedRowsAffected = 0;
			currentBatch = new SqlClientSqlCommandSet();
		}

		protected override void OnPreparedCommand()
		{
			// SQL Server batching can handle several different commands, and
			// that gives us a nice perf boost when mixing different queries for 
			// batching
		}
	}
}