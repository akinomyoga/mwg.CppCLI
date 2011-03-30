#define YIELD_LIST

using Reflector.CodeModel;
using Gen=System.Collections.Generic;
using LanguageWriter=Reflector.Languages.CppCliLanguage.LanguageWriter;

namespace mwg.Reflector.CppCli{
	public struct LabelStatement:IStatement{
		public string label_name;
		internal LabelStatement(string name){
			this.label_name=name;
		}

		#region IHasIlOffset メンバ
		int IHasIlOffset.IlOffset{
			get {return 0;}
			set {}
		}
		int IHasIlOffset.StartIlOffset{
			get {return 0;}
		}
		#endregion
	}
	public struct DeleteStatement:IStatement{
		public IExpression deleteTarget;
		internal DeleteStatement(IExpression target){
			this.deleteTarget=target;
		}

		#region IHasIlOffset メンバ
		int IHasIlOffset.IlOffset{
			get {return 0;}
			set {}
		}
		int IHasIlOffset.StartIlOffset{
			get {return 0;}
		}
		#endregion
	}
	public class LocalRefVariableStatement:IStatement{
		public IVariableDeclaration decl=null;
		public IType var_type=null;
		public string var_name=null;

		public IExpression exp=null;
		public IBlockStatement block=null;

		public string labelname=null;
		public bool noblock=false;

		internal LocalRefVariableStatement(){}
		internal LocalRefVariableStatement(IVariableDeclaration decl,IExpression exp,IBlockStatement block){
			this.decl=decl;
			this.var_type=decl.VariableType;
			this.var_name=decl.Name;
			this.exp=exp;
			this.block=block;
			this.labelname=null;
		}

		public void Write(LanguageWriter w){
			w.PushScope();
			//---------------------------------------------
			if(!this.noblock){
				w.Write("{");
				w.WriteLine();
				w.WriteIndent();
			}
			if(!w.SuppressOutput){
				//*
#warning local-ref: 上層で無駄な宣言を取り除くべき
				if(w.scope.ContainsVariable(this.var_name)){
					w.scope[this.var_name].local_scope=true;
				}else{
					w.scope.RegisterVariable(this.var_name,true);
				}
				//*/
			}

			bool nohandle=this.exp is IObjectCreateExpression; // ハンドル表記でなくても良いかどうか

			this.WriteVariableDecl(w,nohandle);

			// 後に続く内容
			w.WriteLine();
			StatementWriter.WriteBlock(w,this.block);

			// ハンドル表記で宣言した場合はちゃんと delete
			if(!nohandle)
				StatementWriter.WriteStatement(w,new DeleteStatement(new VariableReferenceExpression(this.decl)));

			if(!this.noblock){
				w.WriteOutdent();
				w.Write("}");
				w.WriteLine();
			}
			if(this.labelname!=null) {
				w.__WriteLabel(this.labelname);
				w.Write(";");
				w.WriteLine();
			}
			//---------------------------------------------
			w.PopScope();
		}

		private void WriteVariableDecl(LanguageWriter w,bool nohandle){
			if(nohandle){
				IObjectCreateExpression exp_create=(IObjectCreateExpression)this.exp;

				// 変数型
				IOptionalModifier modopt=this.var_type as IOptionalModifier;
				if(modopt!=null&&modopt.Modifier.Namespace=="System.Runtime.CompilerServices"&&modopt.Modifier.Name=="IsConst") {
					this.var_type=modopt.ElementType;
				}
				new TypeRef(this.var_type).WriteName(w);

				// 変数名
				w.Write(" ");
				w.WriteDeclaration(this.var_name);

				// 初期化子
				if(exp_create.Arguments.Count==0) {
					w.WriteComment(" /* 既定のコンストラクタ */");
				} else {
					w.Write("(");
					w.WriteExpressionCollection(exp_create.Arguments);
					w.Write(")");
				}
			}else{
				// = で代入の場合: ハンドル表記でないと、コンストラクタ呼び出しになってしまう

				// 変数型
				new TypeRef(this.var_type).WriteNameWithRef(w);

				// 変数名
				w.Write(" ");
				w.WriteDeclaration(this.var_name);

				// 代入する値
				w.Write("=");
				ExpressionWriter.WriteExpression(w,this.exp,false);
			}

			w.Write(";");
		}

		#region IHasIlOffset メンバ
		int IHasIlOffset.IlOffset{
			get {return 0;}
			set {}
		}
		int IHasIlOffset.StartIlOffset{
			get {return 0;}
		}
		#endregion

	}

	public struct DefaultConstructionStatement:IStatement{
		public IType var_type;
		public string var_name;

		internal DefaultConstructionStatement(IType type,string name) {
			this.var_type=type;
			this.var_name=name;
		}
		#region IHasIlOffset メンバ
		int IHasIlOffset.IlOffset{
			get {return 0;}
			set {}
		}
		int IHasIlOffset.StartIlOffset{
			get {return 0;}
		}
		#endregion
	}

	public static class StatementAnalyze{
		public static Gen::IEnumerable<IStatement> ModifyStatements(IStatement state){
			IBlockStatement state_block=state as IBlockStatement;
			if(state_block!=null)return ModifyStatements(state_block.Statements);

			return ModifyStatements(new CStatementCollection(state));
		}
		private class CStatementCollection:Gen::List<IStatement>,IStatementCollection{
			public CStatementCollection(IStatement state){
				this.Add(state);
			}

			public void AddRange(System.Collections.ICollection statements) {
				foreach(IStatement state in statements){
					this.Add(state);
				}
			}

			public new void Remove(IStatement value) {
				base.Remove(value);
			}
		}
		public static Gen::IEnumerable<IStatement> ModifyStatements(IStatementCollection collection){
#if YIELD_LIST
			Gen::List<IStatement> yields=new System.Collections.Generic.List<IStatement>();
#endif

			for(int i=0,iM=collection.Count;i<iM;i++){
				IStatement s=collection[i];

				ILabeledStatement state_labeled=s as ILabeledStatement;
				if(state_labeled!=null){
#if YIELD_LIST
					yields.Add(new LabelStatement(state_labeled.Name));
#else
					yield return new LabelStatement(state_labeled.Name);
#endif
					s=state_labeled.Statement;
					if(s==null)continue;
					goto next;
				}

				//
				//	TryCatch から
				//
				ITryCatchFinallyStatement state_tcf=s as ITryCatchFinallyStatement;
				if(state_tcf!=null){
					ModifyCatchClauses(state_tcf);
					goto next;
				}

				//
				//	IUsingStatement → LocalRefVariableStatement 書き込み
				//
				IUsingStatement state_using=s as IUsingStatement;
				if(state_using!=null){
					TransformUsingStatement(yields,state_using,i+1==iM);
					continue;
				}

				//
				//	代入部分から構文合致を始める場合
				//
				IAssignExpression exp_assign=GetAssignExpression(s);
				if(exp_assign!=null){
					IVariableDeclaration var=GetVariable(exp_assign.Target);
					if(var==null)goto next;
					TypeRef type=new TypeRef(var.VariableType);

					//
					// Detect 'default construction of value class'
					//
					//-----------------------------------------------
					// Value value; // default constructor is called
					//-----------------------------------------------
					/*
					if(type.IsValueType){
						IObjectCreateExpression exp_create=exp_assign.Expression as IObjectCreateExpression;
						if(exp_create!=null&&exp_create.Constructor==null){
#if YIELD_LIST
							yields.Add(new DefaultConstructionStatement(exp_create.Type,var.Name));
#else
							yield return new DefaultConstructionStatement(exp_create.Type,var.Name);
#endif
							continue;
						}
					}
					//*/

					//
					// Detect 'delete'
					//
					//-----------------------------------------------
					// IDisposable^ disposable= <expression> ;
					// if(disposable!=nullptr)disposable->Dispose();
					//-----------------------------------------------
					if(i+1<iM&&type.IsType("System","IDisposable")&&DetectDeleteStatement(var.Name,collection[i+1])){
						i++;
#if YIELD_LIST
						yields.Add(new DeleteStatement(exp_assign.Expression));
#else
						yield return new DeleteStatement(exp_assign.Expression);
#endif
						continue;
					}

					//
					// Detect 'local ref value instance'
					//
					//-----------------------------------------------
					// Class value= <expression> ;
					// try{
					//   disposable=value;
					//   ...
					// }...
					// disposable->Dispose();
					//-----------------------------------------------
					LocalRefVariableStatement yret_lrv;
					if(i+2<iM&&DetectLocalRefVariable(var.Name,collection[i+1],collection[i+2],out yret_lrv)){
						i+=2;
						yret_lrv.var_type=var.VariableType;
						yret_lrv.exp=exp_assign.Expression;
#if YIELD_LIST
#warning local-ref: 更に上層でも削除しなければならない可能性がある
						RemoveNullDeclaration(yret_lrv.var_name,yields);
						yields.Add(yret_lrv);
#else
						yield return yret_lrv;
#endif
						continue;
					}

					goto next;
				}
			next:
#if YIELD_LIST
				yields.Add(s);
#else
				yield return s;
#endif
			}

#if YIELD_LIST
			return yields;
#endif
		}

		//===========================================================
		//		細かい関数
		//===========================================================
		private static void RemoveNullDeclaration(string name,Gen::List<IStatement> yields){
			for(int i=0,iM=yields.Count;i<iM;i++){
				IExpressionStatement state_exp=yields[i] as IExpressionStatement;
				if(state_exp==null)continue;

				IVariableDeclarationExpression exp_var;

				IAssignExpression exp_assign=state_exp.Expression as IAssignExpression;
				if(exp_assign!=null){
					// T value=nullptr; の場合
					ILiteralExpression exp_lit=exp_assign.Expression as ILiteralExpression;
					if(exp_lit==null||exp_lit.Value!=null)continue;
					exp_var=exp_assign.Target as IVariableDeclarationExpression;
				}else{
					// T value; の場合
					exp_var=state_exp.Expression as IVariableDeclarationExpression;
				}

				if(exp_var==null||exp_var.Variable.Name!=name)continue;
				
				yields.RemoveAt(i);
				break;
			}
		}
		private static bool IsNullDeclaration(IStatement state){
			IExpressionStatement state_exp=state as IExpressionStatement;
			if(state_exp==null)return false;

			IVariableDeclarationExpression exp_var;

			IAssignExpression exp_assign=state_exp.Expression as IAssignExpression;
			if(exp_assign!=null){
				// T value=nullptr; の場合
				ILiteralExpression exp_lit=exp_assign.Expression as ILiteralExpression;
				if(exp_lit==null||exp_lit.Value!=null)return false;
				exp_var=exp_assign.Target as IVariableDeclarationExpression;
			}else{
				// T value; の場合
				exp_var=state_exp.Expression as IVariableDeclarationExpression;
			}

			return exp_var!=null;
		}
		private static IAssignExpression GetAssignExpression(IStatement statement){
			IExpressionStatement state_exp=statement as IExpressionStatement;
			if(state_exp==null)return null;
			
			return state_exp.Expression as IAssignExpression;
		}
		private static IMethodInvokeExpression GetInvokeExpression(IStatement statement){
			IExpressionStatement state_exp=statement as IExpressionStatement;
			if(state_exp==null)return null;

			return state_exp.Expression as IMethodInvokeExpression;
		}

		[System.Obsolete]
		public static IExpression GetExpression(IStatement statement,out string label){
			ILabeledStatement labeled=statement as ILabeledStatement;
			if(labeled!=null){
				label=labeled.Name;
				statement=labeled.Statement;
			}else{
				label=null;
			}

			IExpressionStatement state_exp=statement as IExpressionStatement;
			if(state_exp!=null)return state_exp.Expression;
			
			return null;
		}
		public static IVariableDeclaration GetVariable(IExpression exp){
			IVariableDeclarationExpression exp_var_decl=exp as IVariableDeclarationExpression;
			if(exp_var_decl!=null)return exp_var_decl.Variable;

			IVariableReferenceExpression exp_var_ref=exp as IVariableReferenceExpression;
			if(exp_var_ref!=null)return exp_var_ref.Variable.Resolve();

			return null;
		}
		public static string GetVariableName(IExpression exp,string nameSpace,string typeName){
			IVariableDeclaration decl=GetVariable(exp);
			if(decl==null||!new TypeRef(decl.VariableType).IsType(nameSpace,typeName))return null;
			return decl.Name;
		}
		public static string GetVariableName(IExpression exp){
			IVariableDeclaration decl=GetVariable(exp);
			if(decl==null)return null;
			return decl.Name;
		}
		//===========================================================
		//		構文合致
		//===========================================================
		/// <summary>
		/// 
		/// </summary>
		/// <param name="var_deleted">削除される予定の変数</param>
		/// <param name="next_state">
		/// 本当に削除命令なのかを確認する為に、次の文を指定。
		/// if(disposable!=nullptr)disposable->Dispose(); なら OK
		/// </param>
		/// <returns>delete だったら true。そうでなかったら、false。</returns>
		private static bool DetectDeleteStatement(string disposable_name,IStatement next_state){
			//---------------------------------------------------
			//		if(disposable==nullptr)
			//---------------------------------------------------
			// if
			IConditionStatement cond=next_state as IConditionStatement;
			if(cond==null) return false;

			// ==
			IBinaryExpression exp_bin=cond.Condition as IBinaryExpression;
			if(exp_bin==null||exp_bin.Operator!=BinaryOperator.IdentityInequality) return false;

			// disposable
			IVariableReferenceExpression exp_var=exp_bin.Left as IVariableReferenceExpression;
			if(exp_var==null||exp_var.Variable.Resolve().Name!=disposable_name) return false;

			// nullptr
			ILiteralExpression exp_lit=exp_bin.Right as ILiteralExpression;
			if(exp_lit==null||exp_lit.Value!=null) return false;

			//---------------------------------------------------
			//		disposable->Dispose();
			//---------------------------------------------------
			// 単文
			IStatementCollection states_then=cond.Then.Statements;
			if(states_then==null||states_then.Count!=1) return false;

			IExpressionStatement state_exp=states_then[0] as IExpressionStatement;
			if(state_exp==null) return false;

			// **->**();
			IMethodInvokeExpression exp_inv=state_exp.Expression as IMethodInvokeExpression;
			if(exp_inv==null||exp_inv.Arguments.Count!=0) return false;

			// **->Dispose();
			IMethodReferenceExpression ref_dispose=exp_inv.Method as IMethodReferenceExpression;
			if(ref_dispose==null||ref_dispose.Method.Name!="Dispose") return false;

			// disposable->Dispose();
			exp_var=ref_dispose.Target as IVariableReferenceExpression;
			if(exp_var==null||exp_var.Variable.Resolve().Name!=disposable_name) return false;

			//---------------------------------------------------
			//		delete disposable
			//---------------------------------------------------
			return true;
		}
		private static bool DetectLocalRefVariable(string var_disposable,IStatement next,IStatement next2,out LocalRefVariableStatement ret_lrv){
			ret_lrv=new LocalRefVariableStatement();
			//-------------------------------------------------------
			// 第二文
			//-------------------------------------------------------
			// try{
			//   ... // null-declarations
			//   var_name=var_disposable;
			//   ...
			// }fault{
			//   var_name->Dispose();
			// }
			ITryCatchFinallyStatement state_next=next as ITryCatchFinallyStatement;
			if(state_next==null
				||state_next.Try==null||state_next.Try.Statements.Count==0
				||state_next.Fault==null||state_next.Fault.Statements.Count!=1
				)return false;

			//
			// null-declarations を飛ばす
			//
			int i_assign=0;
			IStatementCollection try_states=state_next.Try.Statements;
			while(IsNullDeclaration(try_states[i_assign])){
				if(++i_assign>=try_states.Count)return false;
			}

			//
			// var_name=var_disposable の var_name を取得
			//
			IAssignExpression exp_assign=GetAssignExpression(state_next.Try.Statements[i_assign]);
			if(exp_assign==null||var_disposable!=GetVariableName(exp_assign.Expression))return false;
			ret_lrv.var_name=GetVariableName(exp_assign.Target);
			if(ret_lrv.var_name==null)return false;

			//
			// fault 内の形式の確認
			//
			{
				// **->**();
				IMethodInvokeExpression exp_inv=GetInvokeExpression(state_next.Fault.Statements[0]);
				if(exp_inv==null||exp_inv.Arguments.Count!=0)return false;
				// **->Dispose();
				IMethodReferenceExpression method=exp_inv.Method as IMethodReferenceExpression;
				if(method==null||method.Method==null||method.Method.Name!="Dispose")return false;
				// disposable->Dispose();
				if(ret_lrv.var_name!=GetVariableName(method.Target))return false;
			}

			//-------------------------------------------------------
			// 第三文
			//-------------------------------------------------------
			// "Label:"?
			//   var_name->Dispose();
			//
			ILabeledStatement labeled=next2 as ILabeledStatement;
			if(labeled!=null){
				ret_lrv.labelname=labeled.Name;
				next2=labeled.Statement;
			}
			{
				// **->**();
				IMethodInvokeExpression exp_inv=GetInvokeExpression(next2);
				if(exp_inv==null||exp_inv.Arguments.Count!=0)return false;
				// **->Dispose();
				IMethodReferenceExpression method=exp_inv.Method as IMethodReferenceExpression;
				if(method==null||method.Method==null||method.Method.Name!="Dispose")return false;
				// disposable->Dispose();
				if(ret_lrv.var_name!=GetVariableName(method.Target))return false;
			}

			ret_lrv.block=state_next.Try;
			ret_lrv.block.Statements.RemoveAt(i_assign);
			return true;
		}
		//===========================================================
		//		構文合致: catch(...)
		//===========================================================
		private static void ModifyCatchClauses(ITryCatchFinallyStatement state_tcf){
			if(state_tcf.CatchClauses!=null)foreach(ICatchClause clause in state_tcf.CatchClauses){
				TypeRef var_type=new TypeRef(clause.Variable.VariableType);
				if(var_type.IsObject&&clause.Condition!=null) {
					ISnippetExpression iSnippent=clause.Condition as ISnippetExpression;
					if(iSnippent!=null&&iSnippent.Value=="?"){
						IStatementCollection clause_body=clause.Body.Statements;
						if(IsCatchAllClause(ref clause_body)){
							clause.Body=new CBlockStatement(clause_body);
							clause.Condition=new IsCatchAll();
						}
					}
				}
			}
		}
		private struct CBlockStatement:IBlockStatement{
			private IStatementCollection c;
			public CBlockStatement(IStatementCollection states){
				this.c=states;
			}
			public IStatementCollection Statements{get {return this.c;}}
			#region IHasIlOffset メンバ
			int IHasIlOffset.IlOffset{
				get {return 0;}
				set {}
			}
			int IHasIlOffset.StartIlOffset{
				get {return 0;}
			}
			#endregion
		}
		public struct IsCatchAll:IExpression{
			#region IHasIlOffset メンバ
			int IHasIlOffset.IlOffset{
				get {return 0;}
				set {}
			}
			int IHasIlOffset.StartIlOffset{
				get {return 0;}
			}
			#endregion
		}
		public static bool IsCatchAllClause(ref IStatementCollection statements){
			if(statements==null||statements.Count!=3)return false;

			/* uint num1=0;*/{
				IAssignExpression exp_assign=GetAssignExpression(statements[0]);
				if(exp_assign==null||null==GetVariableName(exp_assign.Target,"System","UInt32"))return false;
				ILiteralExpression exp_lit=exp_assign.Expression as ILiteralExpression;
				if(exp_lit==null||(uint)exp_lit.Value!=0u)return false;
			}

			// 以下の二通りの場合がある (他にもあるかも知れない)
			// (1) int num2=::__CxxRegisterExceptionObject((void*)Marshal::GetExceptionPointers(),(void*)num0);
			// (2) ::__CxxRegisterExceptionObject((void*)Marshal::GetExceptionPointers(),(void*)num0);
			// 更に __Cxx の部分は ___Cxx だったりもする
			{
				IExpressionStatement exp_state=statements[1] as IExpressionStatement;
				if(exp_state==null)return false;

				IMethodInvokeExpression exp_inv;
				IAssignExpression exp_assign=exp_state.Expression as IAssignExpression;
				if(exp_assign!=null){
					// (1) に挑戦
					if(null==GetVariableName(exp_assign.Target,"System","Int32"))return false;
					exp_inv=exp_assign.Expression as IMethodInvokeExpression;
				}else{
					// (2) に挑戦
					exp_inv=exp_state.Expression as IMethodInvokeExpression;
				}

				if(exp_inv==null||exp_inv.Arguments.Count!=2)return false;
				IMethodReferenceExpression exp_mref=exp_inv.Method as IMethodReferenceExpression;
				if(exp_mref==null||exp_mref.Method==null||exp_mref.Method.Name.TrimStart('_')!="CxxRegisterExceptionObject")return false;
			}

			//	try{
			//		try{
			//		}catch when{}
			//		break;
			//		if(num!=0u)throw;
			//	}finally{
			//		::__CxxUnregisterExceptionObject(esp0,num);
			//	} 
			ITryCatchFinallyStatement state_tcf=statements[2] as ITryCatchFinallyStatement;
			{
				// try{
				if(state_tcf==null
					||state_tcf.Try==null||state_tcf.Try.Statements.Count!=3
					||state_tcf.Finally==null||state_tcf.Finally.Statements.Count!=1
					)return false;
				
				//     try{
				//	       <statements>
				//	   }catch when{}
				ITryCatchFinallyStatement state_tcf2=state_tcf.Try.Statements[0] as ITryCatchFinallyStatement;
				if(state_tcf2==null||state_tcf2.Try==null)return false;
				
				//     <break;goto;continue;return; 等>

				//     if(**!=**)throw;
				IConditionStatement state_cond=state_tcf.Try.Statements[state_tcf.Try.Statements.Count-1] as IConditionStatement;
				if(state_cond==null||state_cond.Then.Statements.Count!=1)return false;
				IBinaryExpression exp_bin=state_cond.Condition as IBinaryExpression;
				if(exp_bin==null||exp_bin.Operator!=BinaryOperator.ValueInequality) return false;
				IThrowExceptionStatement state_exc=state_cond.Then.Statements[0] as IThrowExceptionStatement;
				if(state_exc==null)return false;

				// } finally {
				//     ::__CxxUnregisterExceptionObject((void*)num2,(int)num);
				// }
				IMethodInvokeExpression exp_inv=GetInvokeExpression(state_tcf.Finally.Statements[0]);
				if(exp_inv==null||exp_inv.Arguments.Count!=2)return false;
				IMethodReferenceExpression exp_mref=exp_inv.Method as IMethodReferenceExpression;
				if(exp_mref==null||exp_mref.Method==null||exp_mref.Method.Name.TrimStart('_')!="CxxUnregisterExceptionObject") return false;

				// <statements>
				statements=state_tcf2.Try.Statements;
				return true;
			}

		}

		//===========================================================
		//		構文合致: function-try-statement
		//		判定が難しい為之は延期
		//		・catch 等から抜ける全てのパスで throw しているか確認しなければならない
		//===========================================================
		public static bool IsFunctionTry(IStatementCollection states){
			if(states==null||states.Count==0)return false;

			// 前に連なっても OK なのは T var=nullptr のみ (この場合には最初の使用時に T も一緒に表示させなければならない...)
			int last=states.Count-1;
			for(int i=0;i<last;i++){
				IExpressionStatement state_exp=states[i] as IExpressionStatement;
				if(state_exp==null)return false;

				IAssignExpression exp_assign=state_exp.Expression as IAssignExpression;
				if(exp_assign==null)return false;

				IVariableDeclaration decl=exp_assign.Target as IVariableDeclaration;
				if(decl==null)return false;

				ILiteralExpression exp_lit=exp_assign.Expression as ILiteralExpression;
				if(exp_lit==null||exp_lit.Value!=null)return false;
			}

			return false;
		}
		//===========================================================
		//		using の変換
		//===========================================================
		/// <summary>
		/// Using 文を他の構文に変換して、yields に変換後の Statement を書き込みます。
		/// </summary>
		/// <param name="yields">変換後の Statement の書き込み先を指定します。</param>
		/// <param name="state">using 構文を表現する Statement を指定します。</param>
		/// <param name="last">state が Statements の中で最後の Statement か否かを指定します。</param>
		public static void TransformUsingStatement(Gen::List<IStatement> yields,IUsingStatement state,bool last){
			// 変数の宣言の場合
			IAssignExpression assig=state.Expression as IAssignExpression;
			if(assig!=null){
				do{
					IVariableDeclarationExpression var_decl_x=assig.Target as IVariableDeclarationExpression;
					if(var_decl_x==null)continue;

					IVariableDeclaration var_decl=var_decl_x.Variable as IVariableDeclaration;
					if(var_decl==null)continue;

					IObjectCreateExpression exp_create=assig.Expression as IObjectCreateExpression;
					if(exp_create!=null){
						LocalRefVariableStatement s_lr=new LocalRefVariableStatement(var_decl,assig.Expression,state.Body);
						s_lr.noblock=last;
						yields.Add(s_lr);
					}else{
						//yields.Add(new ExpressionStatement(assig));
						//yields.Add(state.Body);
						//yields.Add(new DeleteStatement(new VariableReferenceExpression(var_decl)));
						//↑ 中で例外が起こったときのことを考えていない。

						// 宣言部分と代入部分を分離
						IStatement s_decl=new ExpressionStatement(var_decl_x);
						IStatement s_asgn=new ExpressionStatement(
							new AssignExpression(
								new VariableReferenceExpression(var_decl),
								assig.Expression
							)
						);
						IStatement s_delete=new DeleteStatement(new VariableReferenceExpression(var_decl));

						// 宣言
						yields.Add(s_decl);

						// try-finally
						BlockStatement try_block=new BlockStatement();
						try_block.Statements.Add(s_asgn);
						try_block.Statements.AddRange(state.Body.Statements);
						BlockStatement finally_block=new BlockStatement();
						finally_block.Statements.Add(s_delete);
						TryCatchFinallyStatement s_tcf=new TryCatchFinallyStatement(try_block);
						s_tcf.Finally=finally_block;
						yields.Add(s_tcf);
					}
					return;
				}while(false);

				throw new InterfaceNotImplementedException("×実装中×",typeof(IVariableDeclarationExpression),assig.Target);
			}

			// 変数の参照の場合
			IVariableReferenceExpression varref=state.Expression as IVariableReferenceExpression;
			if(varref!=null){
				IStatement s_delete=new DeleteStatement(varref);

				// try-finally
				TryCatchFinallyStatement s_tcf=new TryCatchFinallyStatement(state.Body);
				BlockStatement finally_block=new BlockStatement();
				finally_block.Statements.Add(s_delete);
				s_tcf.Finally=finally_block;
				yields.Add(s_tcf);
				return;
			}

			throw new InterfaceNotImplementedException(
				"Unexpected using-statement expression interface (expects IAssignExpression or IVariableReferenceExpression)",
				typeof(IAssignExpression),state.Expression);
		}
	}
}