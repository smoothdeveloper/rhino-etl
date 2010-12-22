namespace Rhino.Etl.Tests.Joins
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Rhino.Etl.Core;
	using Rhino.Etl.Core.Operations;
	using Rhino.Etl.Core.InlineOperations;
	using Xunit;

	public class InlineJoinOperationEnsureMergerReturnsNewRowTestFixture {

		private static Func<Row, Row, Row> returnsLeftMerger = (left, right) => left;
		private static Func<Row, Row, Row> returnsRightMerger = (left, right) => right;

		FunctorWrapperOperation leftoperation;
		FunctorWrapperOperation rightoperation;
		IEnumerable<Row> leftrows;
		IEnumerable<Row> rightrows;

		public InlineJoinOperationEnsureMergerReturnsNewRowTestFixture() {
			var leftdata = new[] { new { a = 1, b = "a" }, new { a = 2, b = "a" }, new { a = 3, b = "a" } };
			var rightdata = new[] { new { a = 1, c = "a" }, new { a = 2, c = "a" }, new { a = 3, c = "a" } };

			Func<object, IEnumerable<Row>> getRowsFromObjects = r => leftdata.Select(i => Row.FromObject(i));

			leftrows = getRowsFromObjects(leftdata);
			rightrows = getRowsFromObjects(rightdata);

			leftoperation = new FunctorWrapperOperation(_ => leftrows);
			rightoperation = new FunctorWrapperOperation(_ => rightrows);
		}

		[Fact]
		public void WhenTrueAndMergerReturnsEitherLeftOrRightRowAnInvalidOperationIsThrown() {
			string expectedMessagePortion;
			Exception expectedException;
			var columnsToJoinOn = new[] { "a" };

			expectedMessagePortion = "left row == merged";
			expectedException = getCatchExceptionBecauseOfBadRowMerger(columnsToJoinOn, returnsLeftMerger);
			Assert.True(expectedException.Message.Contains(expectedMessagePortion));

			expectedMessagePortion = "right row == merged";
			expectedException = getCatchExceptionBecauseOfBadRowMerger(columnsToJoinOn, returnsRightMerger);
			Assert.True(expectedException.Message.Contains(expectedMessagePortion));
		}

		[Fact]
		public void WhenFalseAndMergerReturnsEitherLeftOrRightRowAnInvalidOperationIsNotThrown() {
			var columnsToJoinOn = new[] { "a" };

			var results = getResults(columnsToJoinOn, returnsLeftMerger);
			foreach (var row in results) {
				Assert.True(leftrows.Contains(row));
			}

			results = getResults(columnsToJoinOn, returnsRightMerger);
			foreach (var row in results) {
				Assert.True(rightrows.Contains(row));
			}
		}

		private IEnumerable<Row> getResults(string[] columnsToJoinOn, Func<Row, Row, Row> rowMerger) {
			var operation = prepareInlineJoinOperation(columnsToJoinOn, rowMerger, false);
			operation.PrepareForExecution(new Rhino.Etl.Core.Pipelines.SingleThreadedPipelineExecuter());
			return operation.Execute(null);
		}

		private InvalidOperationException getCatchExceptionBecauseOfBadRowMerger(string[] columnsToJoinOn, Func<Row, Row, Row> rowMerger) {
			var operation = prepareInlineJoinOperation(columnsToJoinOn, rowMerger, true);
			operation.PrepareForExecution(new Rhino.Etl.Core.Pipelines.SingleThreadedPipelineExecuter());
			return Assert.Throws<InvalidOperationException>(
				() =>
					{
						foreach (var _ in operation.Execute(null))
						{
						}
					});
		}

		private InlineJoinOperation prepareInlineJoinOperation(string[] columnsToJoinOn, Func<Row, Row, Row> rowMerger, bool ensureMergerReturnsNewRow) {
			return new InlineJoinOperation(
				columnsToJoinOn,
				columnsToJoinOn,
				JoinType.Inner,
				rowMerger,
				leftoperation,
				rightoperation,
				ensureMergerReturnsNewRow
				);
		}

		/// <summary>
		/// inject any row provider, useful for unit test as now
		/// </summary>
		public class FunctorWrapperOperation : AbstractOperation {
			private Func<IEnumerable<Row>, IEnumerable<Row>> executor;

			public FunctorWrapperOperation(Func<IEnumerable<Row>, IEnumerable<Row>> executor) {
				this.executor = executor;
			}

			public override IEnumerable<Row> Execute(IEnumerable<Row> rows) {
				return executor(rows);
			}
		}

	}
}