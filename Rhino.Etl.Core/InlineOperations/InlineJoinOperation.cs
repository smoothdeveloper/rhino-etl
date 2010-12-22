namespace Rhino.Etl.Core.InlineOperations {

	using System;
	using Rhino.Etl.Core.Operations;

	/// <summary>
	/// 'Inline' implementation of a <see cref="JoinOperation"/>
	/// </summary>
	public class InlineJoinOperation : JoinOperation {

		private JoinType joinType;
		private string[] leftColumnNames;
		private string[] rightColumnNames;
		private Func<Row, Row, Row> rowMerger;
		private bool ensureMergerReturnsNewRow;

		/// <summary>
		/// initializes a bare <see cref="JoinOperation"/> with everything required
		/// </summary>
		/// <param name="leftColumnNames">column names of the left row to join on</param>
		/// <param name="rightColumnNames">column names of the right row to join on</param>
		/// <param name="joinType"><see cref="JoinType"/> to perform</param>
		/// <param name="rowMerger">A functor reference which takes left and right <see cref="Row"/> and return a merged <see cref="Row"/></param>
		/// <param name="leftOperation">left <see cref="IOperation"/></param>
		/// <param name="rightOperation">right <see cref="IOperation"/></param>
		/// <param name="ensureMergerReturnsNewRow">if true, will throw an <see cref="InvalidOperationException"/> if rowMerger returns any of left or right row</param>
		public InlineJoinOperation(string[] leftColumnNames, string[] rightColumnNames, JoinType joinType, Func<Row, Row, Row> rowMerger, IOperation leftOperation, IOperation rightOperation, bool ensureMergerReturnsNewRow) {
			this.leftColumnNames = leftColumnNames;
			this.rightColumnNames = rightColumnNames;
			this.joinType = joinType;
			this.rowMerger = rowMerger;
			this.ensureMergerReturnsNewRow = ensureMergerReturnsNewRow;
			Left(leftOperation);
			Right(rightOperation);
		}

		/// <summary>
		/// Used on <see cref="LeftOrphanRow"/> if not null
		/// </summary>
		public Action<Row> LeftOrphanHandler { get; set; }
		
		/// <summary>
		/// Used on <see cref="RightOrphanRow"/> if not null
		/// </summary>
		public Action<Row> RightOrphanHandler { get; set; }

		/// <summary>
		/// Allows to define name that is not <c>this.GetType().Name</c> name 
		/// This name is returned by <see cref="Name"/> if not null or empty
		/// </summary>
		public string OverridedName { get; set; }

		/// <inheriteddocs/>
		public override string Name {
			get {
				if(String.IsNullOrEmpty(OverridedName))
					return base.Name;
				return OverridedName;
			}
		}

		protected override Row MergeRows(Row leftRow, Row rightRow) {
			var mergedRow = rowMerger(leftRow, rightRow);
			if (ensureMergerReturnsNewRow) {
				ensureMergedRowDifferentOfInput(leftRow, mergedRow, "left row == mergedRow, do not return left row from passed row merger");
				ensureMergedRowDifferentOfInput(rightRow, mergedRow, "right row == mergedRow, do not return right row from passed row merger");
			}
			return mergedRow;
		}

		private void ensureMergedRowDifferentOfInput(Row inputRow, Row mergedRow, string message) {
			if (inputRow == mergedRow) {
				throw new InvalidOperationException(message);
			}
		}

		protected override void SetupJoinConditions() {
			JoinBuilder joinbuilder;
			switch (joinType) {
				case JoinType.Inner:
					joinbuilder = InnerJoin;
					break;
				case JoinType.Left:
					joinbuilder = LeftJoin;
					break;
				case JoinType.Right:
					joinbuilder = RightJoin;
					break;
				case JoinType.Full:
					joinbuilder = FullOuterJoin;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			joinbuilder
				.Left(leftColumnNames)
				.Right(rightColumnNames);
		}

		protected override void LeftOrphanRow(Row row) {
			if (LeftOrphanHandler != null)
				LeftOrphanHandler(row);
			else
				base.LeftOrphanRow(row);
		}

		protected override void RightOrphanRow(Row row) {
			if (RightOrphanHandler != null)
				RightOrphanHandler(row);
			else
				base.RightOrphanRow(row);
		}
	}
}