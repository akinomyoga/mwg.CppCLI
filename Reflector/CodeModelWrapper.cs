using Gen=System.Collections.Generic;

namespace Reflector.CodeModel{
	/// <summary>
	/// IVariableReferenceExpression ���b�p�ł��B
	/// IVariableReference ���� Expression ���쐬����̂Ɏg�p���܂��B
	/// </summary>
	public class VariableReferenceExpression:IVariableReferenceExpression{
		private IVariableReference reference;
		public IVariableReference Variable {
			get {return this.reference;}
			set {this.reference=value;}
		}
		public VariableReferenceExpression(IVariableReference reference){
			this.reference=reference;
		}

		#region IHasIlOffset �����o
		int IHasIlOffset.IlOffset{
			get {return 0;}
			set {}
		}
		int IHasIlOffset.StartIlOffset{
			get {return 0;}
		}
		#endregion
	}
	/// <summary>
	/// IExpressionStatement ���b�p�ł��B
	/// IExpression ���� IExpressionStatement ���쐬����̂Ɏg�p���܂��B
	/// </summary>
	public struct ExpressionStatement:IExpressionStatement{
		public IExpression exp;
		public ExpressionStatement(IExpression exp){
			this.exp=exp;
		}

		public IExpression Expression {
			get {return this.exp;}
			set {this.exp=value;}
		}

		#region IHasIlOffset �����o
		int IHasIlOffset.IlOffset{
			get {return 0;}
			set {}
		}
		int IHasIlOffset.StartIlOffset{
			get {return 0;}
		}
		#endregion
	}
	public class TryCatchFinallyStatement:ITryCatchFinallyStatement{
		IBlockStatement try_block;
		CatchClauseCollection cc=new CatchClauseCollection();
		IBlockStatement fault_block=null;
		IBlockStatement finally_block=null;

		public TryCatchFinallyStatement(IBlockStatement try_block){
			this.try_block=try_block;
		}

		public ICatchClauseCollection CatchClauses {
			get {return this.cc;}
		}
		public IBlockStatement Try {
			get{return this.try_block;}
			set{this.try_block=value;}
		}
		public IBlockStatement Fault {
			get{return this.fault_block;}
			set{this.fault_block=value;}
		}
		public IBlockStatement Finally {
			get{return this.finally_block;}
			set{this.finally_block=value;}
		}

		private class CatchClauseCollection:Gen::List<ICatchClause>,ICatchClauseCollection{
			public void AddRange(System.Collections.ICollection value) {
				foreach(object o in value){
					ICatchClause clause=o as ICatchClause;
					if(clause==null)continue;
					this.Add(clause);
				}
			}

			public new void Remove(ICatchClause value) {
				base.Remove(value);
			}
		}
		#region IHasIlOffset �����o
		int IHasIlOffset.IlOffset{
			get {return 0;}
			set {}
		}
		int IHasIlOffset.StartIlOffset{
			get {return 0;}
		}
		#endregion
	}
	public class BlockStatement:IBlockStatement{
		private class StatementCollection:Gen::List<IStatement>,IStatementCollection {
			public void AddRange(System.Collections.ICollection statements) {
				foreach(object o in statements){
					IStatement state=o as IStatement;
					if(state==null)continue;
					this.Add(state);
				}
			}

			public new void Remove(IStatement value){
				base.Remove(value);
			}
		}

		StatementCollection statements=new StatementCollection();

		public BlockStatement(){}

		public IStatementCollection Statements {
			get{return this.statements;}
		}

		#region IHasIlOffset �����o
		int IHasIlOffset.IlOffset{
			get {return 0;}
			set {}
		}
		int IHasIlOffset.StartIlOffset{
			get {return 0;}
		}
		#endregion
	}

	/// <summary>
	/// IAssignExpression �̎����ł��B
	/// </summary>
	public class AssignExpression:IAssignExpression{
		private IExpression target;
		private IExpression exp;
		public AssignExpression(IExpression target,IExpression expression){
			this.target=target;
			this.exp=expression;
		}

		public IExpression Expression{
			get{return this.exp;}
			set{this.exp=value;}
		}
		public IExpression Target{
			get{return this.target;}
			set{this.target=value;}
		}
		#region IHasIlOffset �����o
		int IHasIlOffset.IlOffset{
			get {return 0;}
			set {}
		}
		int IHasIlOffset.StartIlOffset{
			get {return 0;}
		}
		#endregion
	}
}